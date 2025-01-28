build:
	@cd ./src; dotnet build --nologo -v q --property WarningLevel=0 /clp:ErrorsOnly

publish-linux: build
	@[ ! -d ./dist ] && mkdir ./dist || rm -f ./dist/*
	@cd ./src; dotnet publish -c Release --sc -r linux-x64 -p:PublishTrimmed=true -p:PublishSingleFile=true -o ../dist/
	@# cd ./src; dotnet publish -c Release --sc -r linux-x64 -p:PublishTrimmed=true -p:PublishSingleFile=true -p:Version=9.9.9.9 -o ../dist/
	@echo 'TODO: update README.md with "git-status --help"'
	@[ -d ~/apps ] && cp ./dist/git-status ~/apps/ || echo "NotFound: ~/apps"

publish-win: build
	@cd ./src; dotnet publish -c Release --sc -r win-x64 -p:PublishTrimmed=true -p:PublishSingleFile=true -o ../dist/

publish: publish-linux publish-win

run: build
	cd ./src; dotnet run

