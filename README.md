# SerialPortPoolService

Un service Windows pour g�rer un pool d'interfaces s�rie de mani�re centralis�e.

## ?? Objectif

Ce service permet de :
- G�rer un pool de ports s�rie disponibles
- Fournir une API REST pour l'allocation/lib�ration des ports
- Monitorer l'�tat des connexions s�rie
- G�rer automatiquement les reconnexions

## ?? Statut du projet

**Sprint actuel : Sprint 1** ? Termin�  
**Prochaine �tape : Sprint 2** - D�couverte des ports s�rie

### Roadmap
- [x] Sprint 1 : Service Windows de base
- [ ] Sprint 2 : D�couverte des ports s�rie
- [ ] Sprint 3 : Pool de connexions
- [ ] Sprint 4 : Interface API REST
- [ ] Sprint 5 : Gestion des erreurs
- [ ] Sprint 6 : Configuration avanc�e

## ?? Installation

### Pr�requis
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

## ?? D�veloppement

```bash
# Compiler
dotnet build

# Tests
dotnet test

# Ex�cution en mode debug
dotnet run --project src/SerialPortPoolService
```

## ?? Monitoring

- **Logs** : `C:\Logs\SerialPortPool\`
- **Event Viewer** : Applications and Services Logs > SerialPortPoolService

## ?? Contribution

Projet d�velopp� en m�thodologie Agile. Voir [CONTRIBUTING.md](docs/CONTRIBUTING.md)

## ?? Licence

MIT License - voir [LICENSE](LICENSE)
