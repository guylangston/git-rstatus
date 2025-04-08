build:
	@cd ./src; dotnet build --nologo -v q --property WarningLevel=0 /clp:ErrorsOnly

publish-linux: build
	@echo " -- Publish: Linux -- "
	@[ ! -d ./dist ] && mkdir ./dist || rm -f ./dist/*
	@cd ./src; dotnet publish -c Release --sc -r linux-x64 -p:PublishTrimmed=true -p:PublishSingleFile=true -o ../dist/
	@echo 'TODO: update README.md with "git-status --help"'
	@[ -d ~/apps ] && cp ./dist/git-rstatus ~/apps/ || echo "NotFound: ~/apps"

publish-win: build
	@echo " -- Publish: Windows -- "
	@cd ./src; dotnet publish -c Release --sc -r win-x64 -p:PublishTrimmed=true -p:PublishSingleFile=true -o ../dist/

publish: publish-linux publish-win

run: build
	cd ./src; dotnet run

