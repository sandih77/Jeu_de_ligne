# Systeme de Sauvegarde - Jeu de Ligne

## Apercu

Le systeme de sauvegarde permet de sauvegarder et charger l'etat complet d'une partie a n'importe quel moment. Il utilise PostgreSQL comme base de donnees.

## Configuration

### Prerequis
- PostgreSQL installe et en cours d'execution
- Base de donnees `jeu_de_ligne` creee

### Installation du Schema
1. Creer la base de donnees:
   ```bash
   psql -U postgres
   CREATE DATABASE jeu_de_ligne;
   \q
   ```

2. Executer le script de schema:
   ```bash
   psql -U postgres -d jeu_de_ligne -f script/schema.sql
   ```

### Connexion
La configuration de connexion se trouve dans `app/utils/UtilsDB.cs`:
```csharp
Host=localhost;Username=postgres;Password=postgres;Database=jeu_de_ligne
```

## Fonctionnalites

### 1. Auto-Save
Active la sauvegarde automatique apres chaque action du joueur:
- Placement d'un point
- Tir de canon

**Activation**: Cochez "Auto-save" dans l'interface

### 2. Sauvegarde Manuelle
Cliquez sur "Sauvegarder" pour:
- Creer une nouvelle partie sauvegardee (premiere fois)
- Ajouter un point de sauvegarde (parties suivantes)

### 3. Chargement
Cliquez sur "Charger" pour:
- Voir toutes les parties sauvegardees
- Selectionner un tour specifique a charger
- Supprimer une sauvegarde

## Architecture de la Base de Donnees

### Tables

```
game_saves
├── id (PK)
├── name
├── board_width, board_height
├── created_at, updated_at
├── is_finished
├── winner_id
└── auto_save

game_players
├── id (PK)
├── game_save_id (FK)
├── player_id
├── name
├── color_r, color_g, color_b
├── score
├── cannon_side
└── cannon_position_y

turn_states
├── id (PK)
├── game_save_id (FK)
├── turn_number
├── current_player_id
├── saved_at
├── action_type
└── description

turn_state_points
├── id (PK)
├── turn_state_id (FK)
├── x, y
├── owner_player_id
└── is_part_of_scored_line

point_scored_directions
├── id (PK)
├── turn_state_point_id (FK)
├── dx, dy

turn_state_lines
├── id (PK)
├── turn_state_id (FK)
├── owner_player_id
└── is_scored

line_points
├── id (PK)
├── turn_state_line_id (FK)
├── x, y
└── point_order

turn_state_scores
├── id (PK)
├── turn_state_id (FK)
├── player_id
└── score
```

### Relations

```
game_saves
    │
    ├──< game_players
    │
    └──< turn_states
            │
            ├──< turn_state_points ──< point_scored_directions
            │
            ├──< turn_state_lines ──< line_points
            │
            └──< turn_state_scores
```

## Flux de Sauvegarde

### Creation d'une Partie
1. L'utilisateur clique sur "Sauvegarder"
2. Un dialogue demande le nom de la partie
3. `CreateGameSaveAsync()` cree l'entree dans `game_saves`
4. Les joueurs sont enregistres dans `game_players`
5. Un ID de partie est attribue pour les sauvegardes suivantes

### Sauvegarde d'un Tour
1. Declenchement (auto ou manuel)
2. `SaveTurnStateAsync()` cree une entree dans `turn_states`
3. Tous les points du plateau sont copies dans `turn_state_points`
4. Les directions scorees sont enregistrees dans `point_scored_directions`
5. Les lignes scorees sont copiees dans `turn_state_lines`
6. Les points des lignes sont copies dans `line_points`
7. Les scores des joueurs sont enregistres dans `turn_state_scores`

### Chargement d'un Tour
1. L'utilisateur selectionne un tour
2. `LoadTurnStateAsync()` reconstruit l'etat du jeu
3. Le plateau est reinitialise avec les points sauvegardes
4. Les lignes scorees sont restaurees
5. Les scores et le joueur courant sont retablis

## Utilisation dans l'Interface

### Boutons
| Bouton | Action |
|--------|--------|
| **Sauvegarder** | Creer/sauvegarder la partie |
| **Charger** | Ouvrir le dialogue de chargement |
| **Auto-save** | Activer la sauvegarde automatique |

### Dialogue de Chargement
- Liste des parties a gauche
- Liste des tours a droite
- Informations sur la partie selectionnee
- Boutons pour charger ou supprimer

## Actions Types Enregistrees

| Code | Description |
|------|-------------|
| `INITIAL` | Etat initial de la partie |
| `POINT_PLACE` | Un joueur place un point |
| `CANNON_FIRE` | Un joueur tire avec le canon |
| `MANUAL_SAVE` | Sauvegarde manuelle |

## Exemple de Code

### Creer une sauvegarde
```csharp
// Dans GameController
var id = await CreateSaveAsync("Ma Partie", autoSave: true);
```

### Charger un tour
```csharp
// Dans GameController
await LoadTurnStateAsync(turnStateId);
```

### Activer l'auto-save
```csharp
_controller.SetAutoSave(true);
```

## Fichiers Concernes

| Fichier | Role |
|---------|------|
| `script/schema.sql` | **Schema SQL de la base de donnees** |
| `app/utils/UtilsDB.cs` | Connexion DB et verification |
| `app/service/GameSaveService.cs` | Logique de sauvegarde |
| `app/model/GameSave.cs` | Modele de partie |
| `app/model/TurnState.cs` | Modele d'etat de tour |
| `app/controller/GameController.cs` | Integration controleur |
| `app/view/SaveGameDialog.cs` | Dialogue de sauvegarde |
| `app/view/LoadGameDialog.cs` | Dialogue de chargement |
| `Form1.cs` | Interface principale |

## Notes

- Les sauvegardes sont independantes: chaque tour contient l'etat complet
- La suppression d'une partie supprime tous les tours associes (CASCADE)
- L'auto-save peut generer beaucoup de donnees sur de longues parties
- Le schema SQL se trouve dans `script/schema.sql`
- Executez le schema manuellement avant la premiere utilisation
