Prérequis PC Démo (Sans Internet)
PC Principal (Demo)
✅ OBLIGATOIRE:
- .NET 9.0 Runtime (https://dotnet.microsoft.com/download/dotnet/9.0)
- Tous les fichiers du dossier RS232Demo/

✅ EXECUTION:
cd RS232Demo/
./RS232Demo.exe
PC Séparé (Dummy UUT)
✅ OBLIGATOIRE:
- Python 3.7+ installé
- pip install pyserial  
- dummy_uut.py
- requirements.txt

✅ EXECUTION:
python dummy_uut.py --port COM8
📋 Checklist Finale
Avant Démo:
bash# ✅ Vérifier sur PC avec internet
dotnet publish tests/RS232Demo/ -c Release -o Demo-Test/
cd Demo-Test/
./RS232Demo.exe  # Doit démarrer et montrer menu

# ✅ Tester Dummy UUT
cd tests/DummyUUT/
python dummy_uut.py --port COM8  # Doit démarrer et être "ONLINE"
Transport:
✅ Copier sur PC démo:
- Dossier RS232Demo/ complet (avec toutes DLLs)
- .NET 9.0 Runtime installer (si pas déjà installé)

✅ Copier sur PC Dummy UUT:  
- dummy_uut.py
- requirements.txt
- Python 3.7+ installer (si pas déjà installé)
🚀 Séquence Démo Recommandée

Setup (5 min avant démo)

Démarrer python dummy_uut.py --port COM8 sur PC séparé
Vérifier "Device State: ONLINE"


Demo (10 minutes)

Lancer RS232Demo.exe sur PC principal
Scenario 5: System Information (montrer discovery)
Scenario 1: Python Dummy UUT (workflow complet)
Scenario 2: Hardware Detection (si FTDI présent)


Success

Workflow 3-phases BIB→UUT→PORT→RS232 fonctionnel
Communication entre 2 PCs démontrée
Architecture SerialPortPool validée