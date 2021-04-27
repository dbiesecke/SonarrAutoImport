all: SonarrAutoImport

SonarrAutoImport: SonarrAutoImport/bin/SonarrAutoImport

SonarrAutoImport/bin/SonarrAutoImport:
	@cd SonarrAutoImport && dotnet publish --configuration Release --output bin

