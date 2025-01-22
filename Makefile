build:
	cd ./src; @dotnet build --nologo -v q --property WarningLevel=0 /clp:ErrorsOnly

build-qf:
	cd ./src; @dotnet build --nologo -v q --property WarningLevel=0 /clp:ErrorsOnly | quickfix-dotnet

publish: build
	cd ./src;

run: build
	cd ./src; dotnet run

