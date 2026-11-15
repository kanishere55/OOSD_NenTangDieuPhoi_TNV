# Đóng gói máy chủ ứng dụng kèm giao diện React đã build thành một image.
# Dùng qua docker compose:  docker compose --profile app up -d --build

# 1. Build giao diện React
FROM node:22-alpine AS giao-dien
WORKDIR /repo/frontend
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci --no-fund --no-audit
COPY frontend/ ./
RUN mkdir -p /repo/backend/Civ09.Api && npm run build

# 2. Build máy chủ ASP.NET Core
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS may-chu
WORKDIR /repo
COPY backend/Civ09.Api/Civ09.Api.csproj backend/Civ09.Api/
RUN dotnet restore backend/Civ09.Api/Civ09.Api.csproj
COPY backend/Civ09.Api/ backend/Civ09.Api/
COPY --from=giao-dien /repo/backend/Civ09.Api/wwwroot backend/Civ09.Api/wwwroot
RUN dotnet publish backend/Civ09.Api/Civ09.Api.csproj -c Release -o /app --no-restore

# 3. Image chạy. Cài phông DejaVu để giấy xác nhận PDF hiện đủ dấu tiếng Việt.
FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update && apt-get install -y --no-install-recommends fonts-dejavu-core fontconfig \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=may-chu /app ./
ENV ASPNETCORE_URLS=http://+:8088
EXPOSE 8088
ENTRYPOINT ["dotnet", "Civ09.Api.dll"]
