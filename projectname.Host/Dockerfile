FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["projectname.Host/projectname.Host.csproj", "projectname.Host/"]
COPY ["Core/Core.csproj", "Core/"]
COPY ["DA/DA.csproj", "DA/"]
COPY ["Utility/Utility.csproj", "Utility/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["Auth/Auth.csproj", "Auth/"]
RUN dotnet restore "projectname.Host/projectname.Host.csproj"
COPY . .
WORKDIR "/src/projectname.Host"
RUN dotnet build "projectname.Host.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "projectname.Host.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "projectname.Host.dll"]