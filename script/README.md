# Installation Base de Donnees

Ce guide explique comment configurer la base de donnees PostgreSQL pour le systeme de sauvegarde.

## Etapes d'Installation

### 1. Installer PostgreSQL
Telechargez et installez PostgreSQL depuis: https://www.postgresql.org/download/

### 2. Creer la Base de Donnees
Ouvrez un terminal et executez:

```bash
psql -U postgres
```

Dans l'invite psql:
```sql
CREATE DATABASE jeu_de_ligne;
\q
```

### 3. Executer le Schema
Depuis le repertoire racine du projet:

```bash
psql -U postgres -d jeu_de_ligne -f script/schema.sql
```

### 4. Verification
Verifiez que les tables ont ete creees:

```bash
psql -U postgres -d jeu_de_ligne
\dt
```

Vous devriez voir les tables suivantes:
- game_saves
- game_players
- turn_states
- turn_state_points
- point_scored_directions
- turn_state_lines
- line_points
- turn_state_scores

## Configuration de la Connexion

La configuration par defaut est definie dans `app/utils/UtilsDB.cs`:

```csharp
Host=localhost;Username=postgres;Password=postgres;Database=jeu_de_ligne
```

Si votre configuration PostgreSQL est differente, modifiez ce fichier.

## Commandes Utiles

### Lister toutes les parties sauvegardees
```sql
SELECT id, name, created_at, auto_save FROM game_saves;
```

### Voir les tours d'une partie
```sql
SELECT turn_number, action_type, description, saved_at
FROM turn_states
WHERE game_save_id = 1
ORDER BY turn_number;
```

### Supprimer toutes les donnees
```sql
TRUNCATE TABLE game_saves CASCADE;
```

### Reinitialiser la base
```bash
psql -U postgres
DROP DATABASE jeu_de_ligne;
CREATE DATABASE jeu_de_ligne;
\q
psql -U postgres -d jeu_de_ligne -f script/schema.sql
```

## Troubleshooting

### "Les tables de la base de donnees n'existent pas"
- Assurez-vous d'avoir execute le script `schema.sql`
- Verifiez que vous etes connecte a la bonne base de donnees

### "Connexion refusee"
- Verifiez que PostgreSQL est en cours d'execution
- Verifiez les parametres de connexion dans `UtilsDB.cs`

### "Authentification echouee"
- Verifiez le mot de passe dans `UtilsDB.cs`
- Modifiez `pg_hba.conf` si necessaire pour autoriser les connexions locales
