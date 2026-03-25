namespace app.service;

using app.model;

public class CannonService
{
    /// <summary>
    /// Calcule la position X cible basee sur la vitesse et la largeur du plateau.
    /// Vitesse 9 atteint la largeur complete du plateau.
    /// </summary>
    public int CalculateTargetX(Cannon cannon, int speed, int boardWidth)
    {
        // Vitesse 1-9 correspond proportionnellement a la largeur du plateau
        // Vitesse 9 = largeur complete, Vitesse 1 = 1/9 de la largeur
        double proportion = speed / 9.0;
        int distance = (int)Math.Floor(boardWidth * proportion) - 1;

        if (cannon.Side == CannonSide.Left)
        {
            // Tire depuis la gauche (x=0) vers la droite
            return Math.Min(distance, boardWidth - 1);
        }
        else
        {
            // Tire depuis la droite (x=boardWidth-1) vers la gauche
            return Math.Max(boardWidth - 1 - distance, 0);
        }
    }

    /// <summary>
    /// Execute le tir du canon et retourne le resultat.
    /// Le tir n'affecte QUE le point a la position X exacte calculee par la vitesse.
    /// Les points traverses avant ne sont pas touches.
    /// </summary>
    public CannonFireResult Fire(Game game, Cannon cannon, int speed)
    {
        int boardWidth = game.Board.Width;
        int targetX = CalculateTargetX(cannon, speed, boardWidth);

        var result = new CannonFireResult(cannon, speed, cannon.PositionY)
        {
            StartX = cannon.Side == CannonSide.Left ? 0 : boardWidth - 1,
            TargetX = targetX
        };

        int y = cannon.PositionY;
        var pos = (targetX, y);
        bool hasHistory = game.DestroyedPoints.ContainsKey(pos);

        // Verifier UNIQUEMENT le point a la position X cible
        var point = game.Board.GetPoint(targetX, y);
        if (point != null && !point.IsEmpty)
        {
            // Peut uniquement toucher les points adverses
            if (point.Owner != cannon.Owner)
            {
                result.HitPoint = point;

                if (point.IsPartOfScoredLine)
                {
                    result.HitInvulnerable = true;
                    result.WasDestroyed = false;
                }
                else if (hasHistory)
                {
                    // Position avec historique - REMPLACER le point par celui du tireur
                    result.WasReplaced = true;
                    result.RestoredForPlayer = cannon.Owner;
                }
                else
                {
                    // Premiere destruction
                    result.WasDestroyed = true;
                }
            }
            // Si c'est son propre point, le tir passe a travers (pas d'effet)
        }
        else if (hasHistory)
        {
            // Position vide avec historique - creer un point pour le tireur
            result.WasRestored = true;
            result.RestoredForPlayer = cannon.Owner;
        }

        return result;
    }

    /// <summary>
    /// Applique le resultat du tir a l'etat du jeu.
    /// </summary>
    public void ApplyFireResult(Game game, CannonFireResult result)
    {
        var pos = (result.TargetX, result.Y);

        if (result.WasDestroyed && result.HitPoint != null)
        {
            // Premiere destruction - stocker et retirer
            game.DestroyedPoints[pos] = result.HitPoint.Owner!;
            game.Board.RemovePoint(result.HitPoint.X, result.HitPoint.Y);
        }
        else if (result.WasReplaced && result.RestoredForPlayer != null)
        {
            // Remplacement - retirer l'ancien et placer le nouveau
            game.Board.RemovePoint(result.TargetX, result.Y);
            game.Board.PlacePoint(result.TargetX, result.Y, result.RestoredForPlayer);
        }
        else if (result.WasRestored && result.RestoredForPlayer != null)
        {
            // Restauration sur position vide
            game.Board.PlacePoint(result.TargetX, result.Y, result.RestoredForPlayer);
        }
    }

    /// <summary>
    /// Valide si le canon peut etre deplace a une position.
    /// </summary>
    public bool CanMoveTo(int newY, int boardHeight)
    {
        return newY >= 0 && newY < boardHeight;
    }

    /// <summary>
    /// Deplace le canon a une nouvelle position Y.
    /// </summary>
    public bool MoveCannon(Cannon cannon, int newY, int boardHeight)
    {
        if (!CanMoveTo(newY, boardHeight))
            return false;

        cannon.PositionY = newY;
        return true;
    }
}
