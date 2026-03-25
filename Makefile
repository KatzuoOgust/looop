.PHONY: help build test format

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
