# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore
COPY ["PROG7311 GLMS MVC/PROG7311GLMS.csproj", "PROG7311 GLMS MVC/"]
RUN dotnet restore "PROG7311 GLMS MVC/PROG7311GLMS.csproj"

# FIX: Array syntax handles the folder spaces flawlessly here
COPY ["PROG7311 GLMS MVC/", "PROG7311 GLMS MVC/"]

# Build
RUN dotnet publish "PROG7311 GLMS MVC/PROG7311GLMS.csproj" -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "PROG7311GLMS.dll"]

#
#Title: Disclosure of AI Usage in my Assessment.
#• Section: MVC Dockerfile.
#• AI Tool: Gemini
#• Purpose/intention : Design assistance of MVC Dockerfile along with lots of troubleshooting.
#• Date(s) 08/06/2026.
#• https://gemini.google.com/share/b712e62df337