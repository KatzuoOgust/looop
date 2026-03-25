.PHONY: help build test format pack push

help: ## Show available targets
	@echo ""
	@echo "Usage:"
	@echo "  make <target>"
	@echo ""
	@echo "Targets:"
	@awk 'BEGIN {FS = ":.*##"} /^[a-zA-Z_-]+:.*?##/ { printf "  \033[36m%-10s\033[0m %s\n", $$1, $$2 }' $(MAKEFILE_LIST)
	@echo ""

build: ## Build the solution
	dotnet build -v q

test: ## Run all tests
	dotnet test -v minimal

format: ## Format code with dotnet format
	dotnet format

pack: ## Pack NuGet packages into artifacts/ (release build)
	dotnet pack Looop.slnx -c Release -o artifacts

push: ## Push artifacts/*.nupkg to NuGet.org (requires NUGET_API_KEY env var)
	dotnet nuget push "artifacts/*.nupkg" \
		--api-key $(NUGET_API_KEY) \
		--source https://api.nuget.org/v3/index.json \
		--skip-duplicate
	dotnet nuget push "artifacts/*.snupkg" \
		--api-key $(NUGET_API_KEY) \
		--source https://api.nuget.org/v3/index.json \
		--skip-duplicate
