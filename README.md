# SerialPortPoolService

Un service Windows pour gérer un pool d'interfaces série de manière centralisée.

## ?? Objectif

Ce service permet de :
- Gérer un pool de ports série disponibles
- Fournir une API REST pour l'allocation/libération des ports
- Monitorer l'état des connexions série
- Gérer automatiquement les reconnexions

## ?? Statut du projet

**Sprint actuel : Sprint 1** ? Terminé  
**Prochaine étape : Sprint 2** - Découverte des ports série

### Roadmap
- [x] Sprint 1 : Service Windows de base
- [ ] Sprint 2 : Découverte des ports série
- [ ] Sprint 3 : Pool de connexions
- [ ] Sprint 4 : Interface API REST
- [ ] Sprint 5 : Gestion des erreurs
- [ ] Sprint 6 : Configuration avancée

## ?? Installation

### Prérequis
- Windows 10/11 ou Windows Server 2016+
- .NET 6.0+
- Droits administrateur

### Installation rapide
```powershell
# Cloner le repository
git clone https://github.com/[USERNAME]/SerialPortPoolService.git
cd SerialPortPoolService

# Compiler
dotnet build --configuration Release

# Installer le service (en tant qu'administrateur)
.\scripts\Install-Service.ps1
```

## ?? Développement

```bash
# Compiler
dotnet build

# Tests
dotnet test

# Exécution en mode debug
dotnet run --project src/SerialPortPoolService
```

## ?? Monitoring

- **Logs** : `C:\Logs\SerialPortPool\`
- **Event Viewer** : Applications and Services Logs > SerialPortPoolService

## ?? Contribution

Projet développé en méthodologie Agile. Voir [CONTRIBUTING.md](docs/CONTRIBUTING.md)

## ?? Licence

MIT License - voir [LICENSE](LICENSE)
