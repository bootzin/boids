run-clean: cleanup restore build run 

all : cleanup restore build

cleanup:
	dotnet clean boids.csproj

restore:
	dotnet restore boids.csproj

build:
	dotnet build boids.csproj

run:
	dotnet run -p boids.csproj