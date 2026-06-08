# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore
COPY ["GLMS_API/GLMS_API.csproj", "GLMS_API/"]
RUN dotnet restore "GLMS_API/GLMS_API.csproj"

# Copy the rest of the API app
COPY GLMS_API/ GLMS_API/

# Build
RUN dotnet publish "GLMS_API/GLMS_API.csproj" -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/wwwroot/uploads
EXPOSE 8080
ENTRYPOINT ["dotnet", "GLMS_API.dll"]


#
#Title: Disclosure of AI Usage in my Assessment.
#• Section: API Dockerfile.
#• AI Tool: Gemini
#• Purpose/intention : Design assistance of API Dockerfile along with lots of troubleshooting.
#• Date(s) 08/06/2026.
#• https://gemini.google.com/share/b712e62df337
