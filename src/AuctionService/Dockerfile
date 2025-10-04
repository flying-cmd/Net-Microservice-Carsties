# uses the .NET 8 SDK image (contains compilers, CLI tools, MSBuild, etc.)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# Sets working directory inside the container to /app
# All following commands (COPY, RUN, etc.) are relative to this folder
WORKDIR /app

# Copies only the solution file (Carsties.sln) and the .csproj files into the container.
COPY Carsties.sln ./
COPY src/AuctionService/AuctionService.csproj ./src/AuctionService/
COPY src/Contracts/Contracts.csproj ./src/Contracts/

# Restores all NuGet dependencies for the AuctionService project (and Contracts, since it’s referenced).
# Uses Docker caching → won’t run again unless .csproj or .sln changes.
# dotnet restore: tells the .NET CLI to download and install all NuGet packages needed for your project
RUN dotnet restore ./src/AuctionService/AuctionService.csproj

# Copies the entire project source code (controllers, entities, etc.) into the container.
COPY . .
# Compiles and publishes your app in Release mode.
# Publishes output (DLLs, configs, static files) into /app/publish.
RUN dotnet publish ./src/AuctionService/AuctionService.csproj -c Release -o /app/publish

# Uses the .NET 8 ASP.NET runtime image (lighter than SDK).
# Contains just enough to run your app (no compilers, no dev tools)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
# the app listens on port 80 inside the container
EXPOSE 80
# Copies the published output from /app/publish in the build stage into /app in the runtime stage
COPY --from=build /app/publish .
# Defines the command that runs when the container starts
# This executes: dotnet AuctionService.dll
ENTRYPOINT ["dotnet", "AuctionService.dll"]