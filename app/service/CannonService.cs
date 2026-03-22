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
        int distance = (int)Math.Round((boardWidth - 1) * proportion);

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
                else
                {
                    result.WasDestroyed = true;
                }
            }
            // Si c'est son propre point, le tir passe a travers (pas d'effet)
        }
        // Si pas de point a cette position, le tir se perd dans le vide

        return result;
    }

    /// <summary>
    /// Applique le resultat du tir a l'etat du jeu.
    /// </summary>
    public void ApplyFireResult(Game game, CannonFireResult result)
    {
        if (result.WasDestroyed && result.HitPoint != null)
        {
            game.Board.RemovePoint(result.HitPoint.X, result.HitPoint.Y);
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
