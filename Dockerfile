# Đóng gói máy chủ ứng dụng kèm giao diện React đã build thành một image.
# Dùng qua docker compose:  docker compose --profile app up -d --build

# 1. Build giao diện React
FROM node:22-alpine AS giao-dien
WORKDIR /repo/web
COPY web/package.json web/package-lock.json ./
RUN npm ci --no-fund --no-audit
COPY web/ ./
RUN mkdir -p /repo/src/Civ09.Api && npm run build

# 2. Build máy chủ ASP.NET Core
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS may-chu
WORKDIR /repo
COPY src/Civ09.Api/Civ09.Api.csproj src/Civ09.Api/
RUN dotnet restore src/Civ09.Api/Civ09.Api.csproj
COPY src/ src/
COPY --from=giao-dien /repo/src/Civ09.Api/wwwroot src/Civ09.Api/wwwroot
RUN dotnet publish src/Civ09.Api/Civ09.Api.csproj -c Release -o /app --no-restore

# 3. Image chạy
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=may-chu /app ./
ENV ASPNETCORE_URLS=http://+:8088
EXPOSE 8088
ENTRYPOINT ["dotnet", "Civ09.Api.dll"]
