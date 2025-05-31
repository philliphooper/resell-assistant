FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Install Node.js
RUN curl -fsSL https://deb.nodesource.com/setup_18.x | bash - && \
    apt-get install -y nodejs

# Copy csproj and restore dependencies
COPY ["Resell Assistant/Resell Assistant.csproj", "Resell Assistant/"]
RUN dotnet restore "Resell Assistant/Resell Assistant.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/Resell Assistant"
RUN dotnet build "Resell Assistant.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Resell Assistant.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Resell Assistant.dll"]
