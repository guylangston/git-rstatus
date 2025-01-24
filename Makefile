build:
	@cd ./src; dotnet build --nologo -v q --property WarningLevel=0 /clp:ErrorsOnly

publish: build
	[ ! -d ./dist ] && mkdir ./dist || rm -f ./dist/*
	cd ./src; dotnet publish -c Release --sc -r linux-x64 -p:PublishTrimmed=true -p:PublishSingleFile=true -o ../dist/
	@# cd ./src; dotnet publish -c Release --sc -r linux-x64 -p:PublishTrimmed=true -p:PublishSingleFile=true -p:Version=9.9.9.9 -o ../dist/
	echo 'TODO: update README.md with "git-status --help"'

run: build
	cd ./src; dotnet run

