# Proportionnalite de la Vitesse du Canon

## Principe de Base

Le canon tire avec une **vitesse de 1 a 9**. Cette vitesse determine **exactement** la position X ou le projectile s'arrete et peut toucher un point adverse.

## Formule de Calcul

```
Position X cible = (Largeur du plateau - 1) × (Vitesse / 9)
```

### Pour le Joueur 1 (canon a gauche, tire vers la droite)
- Le canon est a `X = 0`
- La cible est a `X = distance` calculee

### Pour le Joueur 2 (canon a droite, tire vers la gauche)
- Le canon est a `X = Largeur - 1`
- La cible est a `X = (Largeur - 1) - distance`

---

## Exemples avec un Plateau 10×10

| Vitesse | Proportion | Distance | Position X (J1→) | Position X (←J2) |
|---------|------------|----------|------------------|------------------|
| 1       | 1/9 = 11%  | 1        | X = 1            | X = 8            |
| 2       | 2/9 = 22%  | 2        | X = 2            | X = 7            |
| 3       | 3/9 = 33%  | 3        | X = 3            | X = 6            |
| 4       | 4/9 = 44%  | 4        | X = 4            | X = 5            |
| 5       | 5/9 = 56%  | 5        | X = 5            | X = 4            |
| 6       | 6/9 = 67%  | 6        | X = 6            | X = 3            |
| 7       | 7/9 = 78%  | 7        | X = 7            | X = 2            |
| 8       | 8/9 = 89%  | 8        | X = 8            | X = 1            |
| 9       | 9/9 = 100% | 9        | X = 9            | X = 0            |

---

## Exemples avec un Plateau 20×20

| Vitesse | Proportion | Distance | Position X (J1→) | Position X (←J2) |
|---------|------------|----------|------------------|------------------|
| 1       | 11%        | 2        | X = 2            | X = 17           |
| 3       | 33%        | 6        | X = 6            | X = 13           |
| 5       | 56%        | 11       | X = 11           | X = 8            |
| 7       | 78%        | 15       | X = 15           | X = 4            |
| 9       | 100%       | 19       | X = 19           | X = 0            |

---

## Comportement du Tir

### Ce qui se passe a la position cible

1. **Point adverse non protege** → Le point est **DETRUIT**
2. **Point adverse protege** (fait partie d'une ligne) → Le point est **INVULNERABLE**, tir bloque
3. **Point du meme joueur** → Le tir **passe a travers** (aucun effet)
4. **Aucun point** → Le tir se **perd dans le vide**

### Points traverses avant la cible

Les points situes ENTRE le canon et la position cible ne sont **PAS affectes**.

```
Exemple: Joueur 1 tire avec vitesse 5 sur plateau 10×10

Canon J1                              Position cible (X=5)
   ↓                                        ↓
   [C]----[●]----[○]----[ ]----[ ]----[○]----[ ]----[ ]----[ ]----[C]
   X=0    X=1    X=2    X=3    X=4    X=5    X=6    X=7    X=8    X=9
         (passe) (passe)              (TOUCHE!)

● = Point Joueur 1 (ignore)
○ = Point Joueur 2
[C] = Canon
```

Dans cet exemple:
- Le point a X=2 (adversaire) n'est PAS touche car ce n'est pas la position cible
- Seul le point a X=5 est verifie et potentiellement detruit

---

## Strategie

- **Vitesse faible (1-3)** : Cible les points proches de votre cote
- **Vitesse moyenne (4-6)** : Cible le centre du plateau
- **Vitesse haute (7-9)** : Cible les points proches du cote adverse

Pour toucher un point precis, vous devez:
1. Positionner le canon sur la bonne **ligne Y**
2. Choisir la bonne **vitesse** pour atteindre la colonne X du point cible

---

## Code de Calcul

```csharp
public int CalculateTargetX(Cannon cannon, int speed, int boardWidth)
{
    double proportion = speed / 9.0;
    int distance = (int)Math.Round((boardWidth - 1) * proportion);

    if (cannon.Side == CannonSide.Left)
    {
        return Math.Min(distance, boardWidth - 1);
    }
    else
    {
        return Math.Max(boardWidth - 1 - distance, 0);
    }
}
```
