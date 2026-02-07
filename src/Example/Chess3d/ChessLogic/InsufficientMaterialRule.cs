// from: https://github.com/Geras1mleo/Chess/blob/master/ChessLibrary/ChessBoard/EndGameTypes/InsufficientMaterialRule.cs

// *****************************************************
// *                                                   *
// * O Lord, Thank you for your goodness in our lives. *
// *     Please bless this code to our compilers.      *
// *                     Amen.                         *
// *                                                   *
// *****************************************************
//                                    Made by Geras1mleo

namespace Chess3d.ChessLogic;

using System.Collections.Generic;
using System.Linq;

/// <summary>
/// https://www.chessprogramming.org/Material#InsufficientMaterial
/// </summary>
internal static class InsufficientMaterialRule
{
    internal static bool IsEndGame(Pieces[,] board)
    {
        var pieces = new List<Pieces>();

        for (short i = 0; i < 8; i++)
        {
            for (short j = 0; j < 8; j++)
            {
                if (board[i, j] != Pieces.None)
                    pieces.Add(board[i, j]);
            }
        }

        return IsFirstLevelDraw(pieces)
            || IsSecondLevelDraw(pieces)
            || IsThirdLevelDraw(pieces, board);
    }

    private static bool IsFirstLevelDraw(List<Pieces> pieces)
    {
        return pieces.All(p => p.GetKind() == Pieces.King);
    }

    private static bool IsSecondLevelDraw(List<Pieces> pieces)
    {
        var hasStrongPieces = pieces
            .Any(p => p.GetKind() is Pieces.Pawn or Pieces.Queen or Pieces.Rook);

        // The only piece remaining will be Bishop or Knight, what results in draw
        return !hasStrongPieces && pieces.Count(p => p.GetKind() != Pieces.King) == 1; ;
    }

    private static bool IsThirdLevelDraw(List<Pieces> pieces, Pieces[,] board)
    {
        var isDraw = false;

        if (pieces.Count == 4)
        {
            if (pieces.All(p => p.GetKind() == Pieces.King || p.GetKind() == Pieces.Bishop))
            {
                var firstPiece = pieces.First(p => p.GetKind() == Pieces.Bishop);
                var lastPiece = pieces.Last(p => p.GetKind() == Pieces.Bishop);

                isDraw = firstPiece.GetColor() != lastPiece.GetColor() && BishopsAreOnSameColor(board);
            }
            else if (pieces.All(p => p.GetKind() == Pieces.King || p.GetKind() == Pieces.Knight))
            {
                var firstPiece = pieces.First(p => p.GetKind() == Pieces.Knight);
                var lastPiece = pieces.Last(p => p.GetKind() == Pieces.Knight);

                isDraw = firstPiece.GetColor() == lastPiece.GetColor();
            }
        }

        return isDraw;
    }

    private static bool BishopsAreOnSameColor(Pieces[,] board)
    {
        var bishopsCoords = new List<PiecePosition>();

        for (short i = 0; i < 8 && bishopsCoords.Count < 2; i++)
        {
            for (short j = 0; j < 8 && bishopsCoords.Count < 2; j++)
            {
                if (board[i, j].GetKind() == Pieces.Bishop)
                    bishopsCoords.Add(new PiecePosition(i, j));
            }
        }

        return (bishopsCoords[0].X + bishopsCoords[1].X + bishopsCoords[0].Y + bishopsCoords[1].Y) % 2 == 0;
    }
}