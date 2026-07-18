build:
	@cd ./src; dotnet build --nologo -v q --property WarningLevel=0 /clp:ErrorsOnly

publish-osx: build
	@echo " -- Publish: OSX -- "
	@cd ./src; dotnet publish -c Release --sc -p:PublishSingleFile=true -o ../dist/

publish-linux: build
	@echo " -- Publish: Linux -- "
	@[ ! -d ./dist ] && mkdir ./dist || rm -f ./dist/*
	@cd ./src; dotnet publish -c Release --sc -r linux-x64 -p:PublishTrimmed=true -p:PublishSingleFile=true -o ../dist/
	@cp ./dist/git-rstatus ./dist/git-rstatus--linux-x64-`cat src/git-rstatus.csproj | xq -e "//Version"`
	@echo 'TODO: update README.md with "git-status --help"'
	@[ -d ~/apps ] && cp ./dist/git-rstatus ~/apps/ || echo "NotFound: ~/apps"

publish-win: build
	@echo " -- Publish: Windows -- "
	@cd ./src; dotnet publish -c Release --sc -r win-x64 -p:PublishTrimmed=true -p:PublishSingleFile=true -o ../dist/

publish: publish-linux publish-win

run: build
	cd ./src; dotnet run

