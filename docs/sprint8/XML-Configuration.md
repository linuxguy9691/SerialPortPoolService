# Configuration XML - Sprint 8 Documentation

## 🚀 Aperçu

Cette documentation décrit la syntaxe XML pour configurer les BIB (Board Interface Box) avec le support **regex avancé** introduit dans le **Sprint 8**.

## 📋 Structure Hiérarchique

```
ROOT
├── BIB (Board Interface Box)
│   ├── UUT (Unit Under Test)
│   │   ├── PORT (Port de communication)
│   │   │   ├── PROTOCOL (rs232, rs485, etc.)
│   │   │   ├── START (Phase d'initialisation)
│   │   │   ├── TEST (Phase de test)
│   │   │   └── STOP (Phase d'arrêt)
```

## 🔧 Syntaxe de Base

### Structure XML Complète

```xml
<?xml version="1.0" encoding="UTF-8"?>
<root>
  <bib id="client_demo" description="Démo client production">
    <metadata>
      <version>1.0.0</version>
      <client>Production Client</client>
      <location>Montreal, QC</location>
    </metadata>
    
    <uut id="production_uut" description="UUT de production">
      <metadata>
        <hardware_revision>Rev.2.1</hardware_revision>
        <firmware_version>v1.2.3</firmware_version>
      </metadata>
      
      <port number="1">
        <protocol>rs232</protocol>
        <speed>115200</speed>
        <data_pattern>n81</data_pattern>
        <read_timeout>3000</read_timeout>
        <write_timeout>3000</write_timeout>
        <handshake>None</handshake>
        <rts_enable>false</rts_enable>
        <dtr_enable>true</dtr_enable>
        
        <!-- Commandes de workflow -->
        <start>
          <command>INIT_RS232</command>
          <expected_response regex="true" options="IgnoreCase">(READY|OK|INITIALIZED)</expected_response>
          <timeout_ms>3000</timeout_ms>
          <retry_count>1</retry_count>
        </start>
        
        <test>
          <command>TEST_STATUS</command>
          <expected_response regex="true">^(PASS|SUCCESS).*$</expected_response>
          <timeout_ms>5000</timeout_ms>
        </test>
        
        <stop>
          <command>SHUTDOWN</command>
          <expected_response regex="true">(BYE|GOODBYE|TERMINATED)</expected_response>
          <timeout_ms>2000</timeout_ms>
        </stop>
      </port>
    </uut>
  </bib>
</root>
```

## ✨ Sprint 8 - Nouvelles Fonctionnalités Regex

### Validation par Regex

Le **Sprint 8** introduit le support des **expressions régulières** pour la validation des réponses :

```xml
<expected_response regex="true" options="IgnoreCase,Multiline">
  ^(READY|OK|INITIALIZED).*$
</expected_response>
```

### Attributs Regex

| Attribut | Type | Description | Exemple |
|----------|------|-------------|---------|
| `regex` | `boolean` | Active la validation regex | `regex="true"` |
| `options` | `string` | Options regex (voir tableau ci-dessous) | `options="IgnoreCase"` |

### Options Regex Supportées

| Option | Raccourci | Description | Utilisation |
|--------|-----------|-------------|-------------|
| `IgnoreCase` | `i` | Ignore la casse | Matching insensible à la casse |
| `Multiline` | `m` | Mode multi-ligne (`^` et `$` sur chaque ligne) | Réponses multi-lignes |
| `Singleline` | `s` | `.` matche aussi `\n` | Texte sur plusieurs lignes |
| `ExplicitCapture` | `n` | Seulement les groupes nommés | Performance optimisée |
| `Compiled` | `c` | Compile la regex | Performance pour regex répétées |
| `IgnorePatternWhitespace` | `x` | Ignore espaces dans pattern | Regex complexes et lisibles |

### Exemples de Patterns Regex

#### 1. Validation Simple (Choix Multiple)
```xml
<expected_response regex="true">
  (READY|OK|INITIALIZED)
</expected_response>
```

#### 2. Validation avec Préfixe/Suffixe
```xml
<expected_response regex="true" options="IgnoreCase">
  ^STATUS:\s*(PASS|SUCCESS|OK)\s*$
</expected_response>
```

#### 3. Validation Numérique
```xml
<expected_response regex="true">
  ^TEMP:\s*(\d{1,3})\s*°C$
</expected_response>
```

#### 4. Validation avec Capture de Données
```xml
<expected_response regex="true" options="IgnoreCase">
  ^VERSION:\s*v?(\d+)\.(\d+)\.(\d+)$
</expected_response>
```

#### 5. Validation de Format Complexe
```xml
<expected_response regex="true" options="IgnoreCase,Multiline">
  ^DEVICE_INFO:\s*\n.*ID:\s*([A-F0-9]{8})\s*\n.*STATUS:\s*(ACTIVE|READY)$
</expected_response>
```

## 📝 Backward Compatibility

### Mode String (Ancien Comportement)

```xml
<!-- Sans regex="true" → Validation exacte de string -->
<expected_response>READY</expected_response>
```

### Migration vers Regex

```xml
<!-- AVANT (Sprint 7 et antérieur) -->
<expected_response>READY</expected_response>

<!-- APRÈS (Sprint 8 avec regex) -->
<expected_response regex="true">(READY|OK|INITIALIZED)</expected_response>
```

## 🔧 Configuration des Protocoles

### RS232 (Exemple Complet)

```xml
<port number="1">
  <protocol>rs232</protocol>
  <speed>115200</speed>
  <data_pattern>n81</data_pattern>
  <read_timeout>3000</read_timeout>
  <write_timeout>3000</write_timeout>
  <handshake>None</handshake>
  <rts_enable>false</rts_enable>
  <dtr_enable>true</dtr_enable>
</port>
```

### Vitesses Supportées
`9600`, `19200`, `38400`, `57600`, `115200`, `230400`, `460800`, `921600`

### Patterns de Données
- `n81` : No parity, 8 data bits, 1 stop bit
- `n82` : No parity, 8 data bits, 2 stop bits  
- `e71` : Even parity, 7 data bits, 1 stop bit
- `e72` : Even parity, 7 data bits, 2 stop bits
- `o71` : Odd parity, 7 data bits, 1 stop bit
- `o72` : Odd parity, 7 data bits, 2 stop bits

## 🚨 Validation et Debugging

### Validation au Chargement

Le système valide automatiquement les regex lors du chargement :

```
info: SerialPortPool.Core.Services.XmlBibConfigurationLoader[0]
      📊 Regex pattern detected in XML: (READY|OK|INITIALIZED)
info: SerialPortPool.Core.Services.XmlBibConfigurationLoader[0]
      ✅ Regex pattern validated successfully: (READY|OK|INITIALIZED)
```

### Messages d'Erreur

En cas de regex invalide :

```
error: SerialPortPool.Core.Services.XmlBibConfigurationLoader[0]
       ❌ Invalid regex pattern in XML: [INVALID_PATTERN - Missing closing bracket
```

### Logs de Validation Runtime

```
info: SerialPortPool.Core.Protocols.RS232ProtocolHandler[0]
      ✅ Response validation passed: ✅ VALID [REGEX]: Pattern '(READY|OK|INITIALIZED)' matched 'READY'
```

## 🎯 Bonnes Pratiques

### 1. Utilisez des Patterns Spécifiques
```xml
<!-- ❌ Trop permissif -->
<expected_response regex="true">.*</expected_response>

<!-- ✅ Spécifique et robuste -->
<expected_response regex="true">^(PASS|SUCCESS):\s*\w+$</expected_response>
```

### 2. Gérez les Espaces et Retours à la Ligne
```xml
<!-- ✅ Gère les espaces en début/fin -->
<expected_response regex="true">^\s*(READY|OK)\s*$</expected_response>
```

### 3. Utilisez IgnoreCase pour la Robustesse
```xml
<expected_response regex="true" options="IgnoreCase">
  (ready|ok|initialized)
</expected_response>
```

### 4. Documentez les Patterns Complexes
```xml
<!-- Valide format: "RESULT: PASS (Score: 95)" -->
<expected_response regex="true" options="IgnoreCase">
  ^RESULT:\s*(PASS|FAIL)\s*\(Score:\s*(\d{1,3})\)$
</expected_response>
```

## 🔄 Migration depuis Versions Antérieures

### Étape 1 : Identifier les Validations à Migrer

Recherchez dans votre XML les patterns qui bénéficieraient d'regex :

```bash
# Rechercher les expected_response simples
grep -n "expected_response" config.xml
```

### Étape 2 : Convertir Progressivement

```xml
<!-- AVANT -->
<expected_response>READY</expected_response>

<!-- TRANSITION (compatible mais plus robuste) -->
<expected_response regex="true">\s*READY\s*</expected_response>

<!-- FINAL (validation avancée) -->
<expected_response regex="true" options="IgnoreCase">
  ^(READY|OK|INITIALIZED)\s*$
</expected_response>
```

### Étape 3 : Tester et Valider

```bash
dotnet run --xml-config "config.xml"
```

Vérifiez les logs pour confirmer que les regex fonctionnent correctement.

## 📚 Exemples d'Utilisation Réelle

### Configuration Multi-Ports

```xml
<bib id="multi_port_test">
  <uut id="device_under_test">
    <port number="1">
      <protocol>rs232</protocol>
      <start>
        <command>INIT_PORT1</command>
        <expected_response regex="true">^PORT1:\s*(READY|ACTIVE)$</expected_response>
      </start>
    </port>
    
    <port number="2">
      <protocol>rs485</protocol>
      <start>
        <command>INIT_PORT2</command>
        <expected_response regex="true">^PORT2:\s*(READY|ACTIVE)$</expected_response>
      </start>
    </port>
  </uut>
</bib>
```

## 🛠️ Troubleshooting

### Problèmes Courants

1. **Regex ne matche pas** : Vérifiez les espaces et caractères de contrôle
2. **Performance lente** : Utilisez `options="Compiled"` pour les regex répétées
3. **Caractères spéciaux** : Échappez `\`, `(`, `)`, `[`, `]`, `{`, `}`, etc.

### Debug Tips

Ajoutez des logs temporaires pour voir la réponse exacte :

```xml
<!-- Pattern de debug très permissif -->
<expected_response regex="true">.*</expected_response>
```

Puis regardez les logs pour voir ce qui est reçu et ajustez votre pattern.

---

**Sprint 8** apporte une flexibilité puissante tout en maintenant la compatibilité avec les configurations existantes ! 🚀