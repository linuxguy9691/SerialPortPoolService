# Guide d'Import pour Outils de Test Management

## 📋 **Fichiers Disponibles**

### 1. **testcases_sprint1.csv** (Format CSV universel)
- ✅ **Compatible avec :** TestRail, Jira Xray, Azure DevOps, HP QC/ALM, qTest
- ✅ **Import facile :** Mapping direct des colonnes
- ✅ **Modification simple :** Excel, Google Sheets

### 2. **testcases_sprint1.json** (Format JSON moderne)
- ✅ **Compatible avec :** RestAPI d'outils modernes, TestRail API, Postman
- ✅ **Automatisation :** Scripts d'import personnalisés
- ✅ **CI/CD :** Intégration pipeline

### 3. **testcases_sprint1.xml** (Format XML standard)
- ✅ **Compatible avec :** HP Quality Center, IBM RQM, Micro Focus ALM
- ✅ **Standard :** ISO 29119 compatible
- ✅ **Enterprise :** Outils d'entreprise traditionnels

---

## 🔧 **Instructions d'Import par Outil**

### **TestRail**
1. **Via CSV :**
   - TestRail → Test Cases → Import → Choose CSV file
   - Mapper les colonnes : ID → Case ID, Test Name → Title, etc.
   - Import des test cases dans une nouvelle suite "Sprint 1"

2. **Via API :**
   ```bash
   # Utiliser le fichier JSON avec l'API TestRail
   curl -X POST "https://your-instance.testrail.io/index.php?/api/v2/add_case/1" \
        -u "email:api_key" \
        -H "Content-Type: application/json" \
        -d @testcases_sprint1.json
   ```

### **Jira Xray**
1. **Préparation CSV :**
   - Renommer colonnes selon mapping Xray
   - Test Case ID → Issue Key
   - Description → Summary
   - Test Steps → Test Script

2. **Import :**
   - Jira → Import → CSV → Map fields
   - Choisir "Test" comme Issue Type

### **Azure DevOps**
1. **Test Plans :**
   - Azure DevOps → Test Plans → Import test cases
   - Utiliser le fichier CSV
   - Mapper vers Work Items de type "Test Case"

2. **Colonnes Azure DevOps :**
   ```csv
   ID,Title,Steps,Expected Result,Priority,Area Path,Iteration Path
   ```

### **HP ALM/Quality Center**
1. **Via Excel Add-in :**
   - Ouvrir testcases_sprint1.csv dans Excel
   - Utiliser HP ALM Excel Add-in pour l'import
   - Mapper les champs vers les entités ALM

2. **Via XML Import :**
   - Utiliser testcases_sprint1.xml
   - QC → Requirements → Import → Choose XML

### **qTest Manager**
1. **CSV Import :**
   - qTest → Test Design → Import
   - Choisir CSV format, mapper les colonnes
   - Créer une nouvelle Test Suite "Sprint 1"

---

## 📊 **Mapping des Colonnes Standards**

| Fichier Source | TestRail | Jira Xray | Azure DevOps | HP ALM |
|----------------|----------|-----------|---------------|--------|
| Test Case ID | Case ID | Issue Key | ID | Test ID |
| Test Name | Title | Summary | Title | Test Name |
| Priority | Priority | Priority | Priority | Priority |
| Test Steps | Steps | Test Script | Steps | Design Steps |
| Expected Result | Expected | Expected Results | Expected Result | Expected |
| Status | Status | Execution Status | Outcome | Status |

---

## 🔄 **Scripts d'Import Automatiques**

### **PowerShell - TestRail API**
```powershell
# Import vers TestRail via API
$jsonData = Get-Content "testcases_sprint1.json" | ConvertFrom-Json
$headers = @{
    "Authorization" = "Basic " + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("email:api_key"))
    "Content-Type" = "application/json"
}

foreach ($testCase in $jsonData.testCases) {
    $body = @{
        "title" = $testCase.title
        "priority_id" = 2  # Medium
        "custom_steps" = $testCase.testSteps
    } | ConvertTo-Json

    Invoke-RestMethod -Uri "https://instance.testrail.io/index.php?/api/v2/add_case/1" -Method POST -Headers $headers -Body $body
}
```

### **Python - Jira Xray**
```python
import pandas as pd
import requests

# Lire le CSV
df = pd.read_csv('testcases_sprint1.csv')

# Connexion Jira
auth = ('user@example.com', 'api_token')
headers = {'Content-Type': 'application/json'}

for index, row in df.iterrows():
    issue_data = {
        "fields": {
            "project": {"key": "PROJ"},
            "summary": row['Test Name'],
            "issuetype": {"name": "Test"},
            "priority": {"name": row['Priority']},
            "description": row['Description']
        }
    }
    
    response = requests.post(
        'https://company.atlassian.net/rest/api/3/issue',
        auth=auth,
        headers=headers,
        json=issue_data
    )
```

---

## 📋 **Checklist avant Import**

- [ ] **Backup existant :** Sauvegarder les données actuelles
- [ ] **Permissions :** Vérifier les droits d'import
- [ ] **Mapping :** Valider le mapping des colonnes
- [ ] **Test import :** Essayer avec 1-2 test cases d'abord
- [ ] **Validation :** Vérifier que tous les champs sont importés
- [ ] **Liens :** Recréer les liens entre test cases si nécessaire

---

## 🛠️ **Customisation par Projet**

### **Ajout de champs spécifiques :**
1. **Traceability :** Ajouter liens vers User Stories
2. **Environment :** Spécifier l'environnement de test
3. **Component :** Mapper vers les composants système
4. **Labels/Tags :** Catégoriser les tests

### **Exemple customisation TestRail :**
```csv
Test Case ID,Title,Suite,Priority,Type,Component,User Story,Environment
TC-001,Compilation Debug,Build,High,Functional,Core,US-001,Development
```

---

## 📞 **Support**

Si vous rencontrez des problèmes d'import :

1. **Vérifier la documentation** de votre outil de test management
2. **Ajuster le mapping** des colonnes selon vos besoins
3. **Tester l'import** avec un petit échantillon d'abord
4. **Contacter le support** de votre outil si nécessaire

**Les fichiers fournis couvrent 13 test cases complets du Sprint 1 avec tous les détails nécessaires pour une traçabilité complète !** ✅
