using System.Data;
using Civ09.Api.DuLieu;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Civ09.Api.HoTro;

// Bảo đảm một thao tác được lưu đầy đủ hoặc hoàn tác toàn bộ khi có lỗi (BR10).
public class GiaoDich
{
    private readonly KhoNghiepVu _kho;

    public GiaoDich(KhoNghiepVu kho) => _kho = kho;

    public async Task<T> ThucHienAsync<T>(Func<Task<T>> thaoTac)
    {
        await using var gd = await _kho.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var ketQua = await thaoTac();
            await _kho.SaveChangesAsync();
            await gd.CommitAsync();
            return ketQua;
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && (sql.Number == 2601 || sql.Number == 2627))
        {
            await gd.RollbackAsync();
            throw LoiNghiepVu.XungDot("Dữ liệu bị trùng với một bản ghi đang có.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && sql.Number is >= 50000 and < 51000)
        {
            // Trigger trong CSDL từ chối thay đổi; thông báo của trigger được trả lại cho người dùng.
            // Lỗi trong trigger đã làm SQL Server hoàn tác giao dịch, nên bỏ qua lỗi khi hoàn tác lần nữa.
            try { await gd.RollbackAsync(); } catch (InvalidOperationException) { }
            throw LoiNghiepVu.XungDot(sql.Message);
        }
        catch (Exception ex) when (TimLoiSql(ex) is { Number: 1205 })
        {
            try { await gd.RollbackAsync(); } catch (InvalidOperationException) { }
            throw LoiNghiepVu.XungDot("Có thao tác khác đang xử lý cùng dữ liệu. Vui lòng thử lại.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
        {
            await gd.RollbackAsync();
            throw LoiNghiepVu.KhongHopLe("Dữ liệu không thỏa ràng buộc của cơ sở dữ liệu.");
        }
        catch
        {
            try { await gd.RollbackAsync(); } catch (InvalidOperationException) { }
            throw;
        }
    }

    private static SqlException? TimLoiSql(Exception? ex)
    {
        for (; ex != null; ex = ex.InnerException)
            if (ex is SqlException sql) return sql;
        return null;
    }

    public Task ThucHienAsync(Func<Task> thaoTac) => ThucHienAsync(async () => { await thaoTac(); return true; });

    // Khóa một dòng đến hết giao dịch để các yêu cầu đồng thời trên cùng dòng phải chờ lần lượt.
    // Tên bảng và cột là hằng số trong mã, chỉ giá trị khóa là tham số.
#pragma warning disable EF1002 // tên bảng, cột là hằng số trong mã; giá trị khóa đi qua tham số
    public Task KhoaDongAsync(string bang, string cotKhoa, string giaTri)
        => _kho.Database.ExecuteSqlRawAsync(
            $"SELECT {cotKhoa} FROM dbo.{bang} WITH (UPDLOCK, HOLDLOCK, ROWLOCK) WHERE {cotKhoa} = {{0}}", giaTri);
#pragma warning restore EF1002
}
