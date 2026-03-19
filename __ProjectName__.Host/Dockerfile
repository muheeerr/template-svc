FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["__ProjectName__.Host/__ProjectName__.Host.csproj", "__ProjectName__.Host/"]
COPY ["Core/Core.csproj", "Core/"]
COPY ["DA/DA.csproj", "DA/"]
COPY ["Utility/Utility.csproj", "Utility/"]
RUN dotnet restore "__ProjectName__.Host/__ProjectName__.Host.csproj"
COPY . .
WORKDIR "/src/__ProjectName__.Host"
RUN dotnet build "__ProjectName__.Host.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "__ProjectName__.Host.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "__ProjectName__.Host.dll"]