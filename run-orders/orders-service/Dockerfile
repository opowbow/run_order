# Use the official .NET Core SDK image as the base image
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

# Set the working directory
WORKDIR /app

# Copy the project files into the container
COPY . .

# Restore the project's dependencies
RUN dotnet restore

# Build the project
RUN dotnet build

# Publish the project
RUN dotnet publish -c Release -o out

# Use the published output as the final image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build /app/out .

# Expose port 80 for the web API to listen on
EXPOSE 80

# Start the web API when the container runs
ENTRYPOINT ["dotnet", "orders-service.dll"]
