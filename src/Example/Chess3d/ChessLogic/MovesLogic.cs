namespace Chess3d.ChessLogic;

using System;
using System.Collections.Generic;
using System.Linq;

public class MovesLogic
{
    private readonly Pieces[,] Board = new Pieces[8, 8];
    private BoardState BoardState;
    private int EnPassant; // -1 means no en passant available, otherwise stores the x position of the pawn that can be captured en passant

    private bool Castling_WhiteKingSide;
    private bool Castling_WhiteQueenSide;
    private bool Castling_BlackKingSide;
    private bool Castling_BlackQueenSide;

    public bool IsWhiteKingChecked
    {
        get => isWhiteKingChecked;
        private set
        {
            if (value != isWhiteKingChecked)
            {
                isWhiteKingChecked = value;
                //OnWhiteKingCheckedChangedEvent(value);
            }
        }
    }
    private bool isWhiteKingChecked = false;

    public bool IsBlackKingChecked
    {
        get => isBlackKingChecked;
        private set
        {
            if (value != isBlackKingChecked)
            {
                isBlackKingChecked = value;
                //OnBlackKingCheckedChangedEvent(value);
            }
        }
    }
    private bool isBlackKingChecked = false;

    public void InitBoard()
    {
        for (int i = 0; i < 8; i++)
        {
            for (int j = 2; j < 6; j++)
            {
                Board[j, i] = Pieces.None;
            }
        }

        Castling_WhiteKingSide = true;
        Castling_WhiteQueenSide = true;
        Castling_BlackKingSide = true;
        Castling_BlackQueenSide = true;
        BoardState = BoardState.WhiteTurn;
        IsWhiteKingChecked = false;
        IsBlackKingChecked = false;
        EnPassant = -1;

        for (int x = 0; x < 8; x++)
        {
            Board[1, x] = Pieces.Pawn | Pieces.White;
            Board[6, x] = Pieces.Pawn | Pieces.Black;
        }

        Board[0, 0] = Pieces.Rook | Pieces.White;
        Board[0, 7] = Pieces.Rook | Pieces.White;
        Board[7, 0] = Pieces.Rook | Pieces.Black;
        Board[7, 7] = Pieces.Rook | Pieces.Black;

        Board[0, 1] = Pieces.Knight | Pieces.White;
        Board[0, 6] = Pieces.Knight | Pieces.White;
        Board[7, 1] = Pieces.Knight | Pieces.Black;
        Board[7, 6] = Pieces.Knight | Pieces.Black;

        Board[0, 2] = Pieces.Bishop | Pieces.White;
        Board[0, 5] = Pieces.Bishop | Pieces.White;
        Board[7, 2] = Pieces.Bishop | Pieces.Black;
        Board[7, 5] = Pieces.Bishop | Pieces.Black;

        Board[0, 3] = Pieces.Queen | Pieces.White;
        Board[7, 3] = Pieces.Queen | Pieces.Black;

        Board[0, 4] = Pieces.King | Pieces.White;
        Board[7, 4] = Pieces.King | Pieces.Black;
    }

    // Validate basic piece movement rules (does not consider checks)
    private bool IsValidMove(Move move, Pieces piece, out bool isCastling)
    {
        isCastling = false;
        var color = piece.GetColor();
        var pieceKind = piece.GetKind();
        var targetPiece = Board[move.To.Y, move.To.X];
        // Cannot capture own piece
        if (targetPiece.GetColor() == color) return false;

        int dx = move.To.X - move.From.X;
        int dy = move.To.Y - move.From.Y;
        int adx = Math.Abs(dx);
        int ady = Math.Abs(dy);

        // Pawn movement
        if (pieceKind == Pieces.Pawn)
        {
            int dir = (color == Pieces.White) ? 1 : -1;

            // Vertical move
            if (dx == 0)
            {
                // One step forward into empty
                if (dy == dir && targetPiece == Pieces.None) return true;
                // Two steps from initial position
                var startRow = (color == Pieces.White) ? 1 : 6;
                if (dy == 2 * dir && move.From.Y == startRow)
                {
                    // path clear
                    var intermediate = new PiecePosition(move.From.X, move.From.Y + dir);
                    if (Board[intermediate.Y, intermediate.X] == Pieces.None && targetPiece == Pieces.None)
                        return true;
                }
                return false;
            }

            // Captures
            if (ady == 1 && dy == dir && adx == 1)
            {
                // Normal capture
                if (targetPiece != Pieces.None && targetPiece.GetColor() != color) return true;

                // En passant: target square empty but EnPassant allows capture
                if (targetPiece == Pieces.None && EnPassant >= 0 && EnPassant == move.To.X)
                {
                    // From must be on the correct rank to capture en passant
                    var requiredFromY = (color == Pieces.White) ? 4 : 3;
                    if (move.From.Y == requiredFromY) return true;
                }
            }

            return false;
        }

        // Knight
        if (pieceKind == Pieces.Knight)
        {
            if ((adx == 1 && ady == 2) || (adx == 2 && ady == 1)) return true;
            return false;
        }

        // Bishop
        if (pieceKind == Pieces.Bishop)
        {
            if (adx == ady && adx > 0)
            {
                return IsPathClear(Board, move.From, move.To);
            }
            return false;
        }

        // Rook
        if (pieceKind == Pieces.Rook)
        {
            if ((adx == 0 && ady > 0) || (ady == 0 && adx > 0))
            {
                return IsPathClear(Board, move.From, move.To);
            }
            return false;
        }

        // Queen
        if (pieceKind == Pieces.Queen)
        {
            if ((adx == ady && adx > 0) || (adx == 0 && ady > 0) || (ady == 0 && adx > 0))
            {
                return IsPathClear(Board, move.From, move.To);
            }
            return false;
        }

        // King
        if (pieceKind == Pieces.King)
        {
            // Normal one-square moves
            if (adx <= 1 && ady <= 1) return true;

            // Castling: king moves two squares horizontally
            if (ady == 0 && adx == 2)
            {
                isCastling = true;
                var opponent = (color == Pieces.White) ? Pieces.Black : Pieces.White;

                // White castling (row 0)
                if (color == Pieces.White && move.From.Y == 0)
                {
                    // King-side
                    if (move.To.X == 6)
                    {
                        if (!Castling_WhiteKingSide) return false;
                        // Rook must be on (7,0)
                        var rook = Board[0, 7];
                        if (rook.GetKind() != Pieces.Rook || rook.GetColor() != color) return false;
                        // Path squares (5,0) and (6,0) must be empty
                        if (Board[0, 5] != Pieces.None || Board[0, 6] != Pieces.None) return false;
                        // King must not be in check, and squares it passes through must not be attacked
                        if (IsSquareAttacked(Board, new PiecePosition(4, 0), opponent)) return false;
                        if (IsSquareAttacked(Board, new PiecePosition(5, 0), opponent)) return false;
                        if (IsSquareAttacked(Board, new PiecePosition(6, 0), opponent)) return false;

                        return true;
                    }

                    // Queen-side
                    if (move.To.X == 2)
                    {
                        if (!Castling_WhiteQueenSide) return false;
                        var rook = Board[0, 0];
                        if (rook.GetKind() != Pieces.Rook || rook.GetColor() != color) return false;
                        // Path squares (1,0),(2,0),(3,0) must be empty
                        if (Board[0, 1] != Pieces.None || Board[0, 2] != Pieces.None || Board[0, 3] != Pieces.None) return false;
                        // King must not be in check, and squares it passes through must not be attacked (4,3,2)
                        if (IsSquareAttacked(Board, new PiecePosition(4, 0), opponent)) return false;
                        if (IsSquareAttacked(Board, new PiecePosition(3, 0), opponent)) return false;
                        if (IsSquareAttacked(Board, new PiecePosition(2, 0), opponent)) return false;

                        return true;
                    }
                }

                // Black castling (row 7)
                if (color == Pieces.Black && move.From.Y == 7)
                {
                    // King-side
                    if (move.To.X == 6)
                    {
                        if (!Castling_BlackKingSide) return false;
                        var rook = Board[7, 7];
                        if (rook.GetKind() != Pieces.Rook || rook.GetColor() != color) return false;
                        if (Board[7, 5] != Pieces.None || Board[7, 6] != Pieces.None) return false;
                        if (IsSquareAttacked(Board, new PiecePosition(4, 7), opponent)) return false;
                        if (IsSquareAttacked(Board, new PiecePosition(5, 7), opponent)) return false;
                        if (IsSquareAttacked(Board, new PiecePosition(6, 7), opponent)) return false;

                        return true;
                    }

                    // Queen-side
                    if (move.To.X == 2)
                    {
                        if (!Castling_BlackQueenSide) return false;
                        var rook = Board[7, 0];
                        if (rook.GetKind() != Pieces.Rook || rook.GetColor() != color) return false;
                        if (Board[7, 1] != Pieces.None || Board[7, 2] != Pieces.None || Board[7, 3] != Pieces.None) return false;
                        if (IsSquareAttacked(Board, new PiecePosition(4, 7), opponent)) return false;
                        if (IsSquareAttacked(Board, new PiecePosition(3, 7), opponent)) return false;
                        if (IsSquareAttacked(Board, new PiecePosition(2, 7), opponent)) return false;

                        return true;
                    }
                }

                return false;
            }

            return false;
        }

        return false;
    }

    // Check whether the move would leave own king in check
    private bool IsKingCheckedValidation(Move move)
    {
        // Create a copy of the board to simulate the move
        var temp = (Pieces[,])Board.Clone();

        var fromY = move.From.Y;
        var fromX = move.From.X;
        var toY = move.To.Y;
        var toX = move.To.X;

        var movingPiece = temp[fromY, fromX];
        var color = movingPiece.GetColor();
        var pieceKind = movingPiece.GetKind();

        // Apply move
        temp[toY, toX] = movingPiece;
        temp[fromY, fromX] = Pieces.None;

        // Handle en passant captured pawn removal in simulation
        if (pieceKind == Pieces.Pawn)
        {
            int dx = toX - fromX;
            int dy = toY - fromY;
            if (Math.Abs(dx) == 1 && dy == ((color == Pieces.White) ? 1 : -1) && temp[toY, toX] != Pieces.None)
            {
                // normal capture already applied
            }
            else if (Math.Abs(dx) == 1 && dy == ((color == Pieces.White) ? 1 : -1) && temp[toY, toX] == Pieces.None && EnPassant >= 0 && EnPassant == toX)
            {
                // remove the pawn that was captured en passant (it sits on the fromY row)
                temp[fromY, toX] = Pieces.None;
            }
        }

        // Special handling for castling: when king moves two squares, also move the rook in the simulated board
        if (pieceKind == Pieces.King && Math.Abs(toX - fromX) == 2 && fromY == toY)
        {
            // Determine rook source/target depending on side
            if (toX == 6) // king-side
            {
                // rook moves from (7,fromY) to (5,fromY)
                temp[fromY, 5] = temp[fromY, 7];
                temp[fromY, 7] = Pieces.None;
            }
            else if (toX == 2) // queen-side
            {
                // rook moves from (0,fromY) to (3,fromY)
                temp[fromY, 3] = temp[fromY, 0];
                temp[fromY, 0] = Pieces.None;
            }
        }

        // Find our king position
        PiecePosition kingPos = new(-1, -1);
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                var p = temp[y, x];
                if (p.GetKind() == Pieces.King && p.GetColor() == color)
                {
                    kingPos = new PiecePosition(x, y);
                    break;
                }
            }
            if (kingPos.X != -1) break;
        }

        if (kingPos.X == -1)
        {
            // Shouldn't happen, but if no king found consider it invalid (in check)
            return true;
        }

        // Now check if any opposing piece attacks kingPos on temp board
        var opponentColor = (color == Pieces.White) ? Pieces.Black : Pieces.White;
        return IsSquareAttacked(temp, kingPos, opponentColor);
    }

    // Check if any piece of attackerColor on board 'temp' attacks target
    private static bool IsSquareAttacked(Pieces[,] temp, PiecePosition target, Pieces attackerColor)
    {
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                var p = temp[y, x];
                if (p == Pieces.None) continue;
                if (p.GetColor() != attackerColor) continue;

                var pieceKind = p.GetKind();
                int dx = target.X - x;
                int dy = target.Y - y;
                int adx = Math.Abs(dx);
                int ady = Math.Abs(dy);

                // Pawn attacks
                if (pieceKind == Pieces.Pawn)
                {
                    int dir = (attackerColor == Pieces.White) ? 1 : -1;
                    if (dy == dir && Math.Abs(dx) == 1) return true;
                    continue;
                }

                // Knight
                if (pieceKind == Pieces.Knight)
                {
                    if ((adx == 1 && ady == 2) || (adx == 2 && ady == 1)) return true;
                    continue;
                }

                // Bishop / Queen (diagonal)
                if (pieceKind is Pieces.Bishop or Pieces.Queen)
                {
                    if (adx == ady && adx > 0)
                    {
                        if (IsPathClear(temp, new PiecePosition(x, y), target)) return true;
                    }
                }

                // Rook / Queen (straight)
                if (pieceKind is Pieces.Rook or Pieces.Queen)
                {
                    if ((adx == 0 && ady > 0) || (ady == 0 && adx > 0))
                    {
                        if (IsPathClear(temp, new PiecePosition(x, y), target)) return true;
                    }
                }

                // King
                if (pieceKind == Pieces.King)
                {
                    if (Math.Max(adx, ady) == 1) return true;
                }
            }
        }
        return false;
    }

    // Check clear path for sliding pieces (excluding from and to)
    private static bool IsPathClear(Pieces[,] board, PiecePosition from, PiecePosition to)
    {
        int dx = Math.Sign(to.X - from.X);
        int dy = Math.Sign(to.Y - from.Y);

        int x = from.X + dx;
        int y = from.Y + dy;
        while (x != to.X || y != to.Y)
        {
            if (board[y, x] != Pieces.None) return false;
            x += dx;
            y += dy;
        }
        return true;
    }

    public (bool,List<BoardUpdate>?) TryMovePiece(PiecePosition p1, PiecePosition p2)
    {
        var movingPiece = GetPieceAt(p1);
        if (movingPiece == Pieces.None)
            return (false, null);

        var movingColor = movingPiece.GetColor();
        var movingKind = movingPiece.GetKind();
        var targetPiece = GetPieceAt(p2);

        var move = new Move(p1, p2);

        // --- VALIDATION (must be PURE) ---
        if (!IsValidMove(move, movingPiece, out _))
            return (false, null);

        if (IsKingCheckedValidation(move))
            return (false, null);

        List<BoardUpdate> updates = [];

        // --- EN PASSANT ---
        if (movingKind == Pieces.Pawn &&
            targetPiece == Pieces.None &&
            Math.Abs(p2.X - p1.X) == 1)
        {
            int direction = movingColor == Pieces.White ? 1 : -1;

            if (p2.Y - p1.Y == direction &&
                EnPassant == p2.X)
            {
                int capturedPawnY = p2.Y - direction;
                var capturedPawnPos = new PiecePosition(p2.X, capturedPawnY);

                var capturedPawn = GetPieceAt(capturedPawnPos);
                if (capturedPawn.GetKind() == Pieces.Pawn &&
                    capturedPawn.GetColor() != movingColor)
                {
                    Board[capturedPawnY, p2.X] = Pieces.None;

                    updates.Add(new BoardUpdate(
                        BoardUpdateType.Remove,
                        capturedPawnPos,
                        default));
                }
            }
        }

        // --- NORMAL CAPTURE ---
        if (targetPiece != Pieces.None)
        {
            updates.Add(new BoardUpdate(
                BoardUpdateType.Remove,
                p2,
                default));
        }

        // --- MOVE PIECE ---
        Board[p2.Y, p2.X] = movingPiece;
        Board[p1.Y, p1.X] = Pieces.None;

        updates.Add(new BoardUpdate(
            BoardUpdateType.Move,
            p1,
            p2));

        // --- CASTLING ---
        if (movingKind == Pieces.King &&
            Math.Abs(p2.X - p1.X) == 2 &&
            p1.Y == p2.Y)
        {
            if (p2.X == 6) // king-side
            {
                var rookFrom = new PiecePosition(7, p1.Y);
                var rookTo = new PiecePosition(5, p1.Y);

                Board[p1.Y, 5] = Board[p1.Y, 7];
                Board[p1.Y, 7] = Pieces.None;

                updates.Add(new BoardUpdate(
                    BoardUpdateType.Move,
                    rookFrom,
                    rookTo));
            }
            else if (p2.X == 2) // queen-side
            {
                var rookFrom = new PiecePosition(0, p1.Y);
                var rookTo = new PiecePosition(3, p1.Y);

                Board[p1.Y, 3] = Board[p1.Y, 0];
                Board[p1.Y, 0] = Pieces.None;

                updates.Add(new BoardUpdate(
                    BoardUpdateType.Move,
                    rookFrom,
                    rookTo));
            }
        }

        // --- CASTLING RIGHTS ---
        if (movingKind == Pieces.King)
        {
            if (movingColor == Pieces.White)
            {
                Castling_WhiteKingSide = false;
                Castling_WhiteQueenSide = false;
            }
            else
            {
                Castling_BlackKingSide = false;
                Castling_BlackQueenSide = false;
            }
        }

        if (movingKind == Pieces.Rook)
        {
            if (movingColor == Pieces.White && p1.Y == 0)
            {
                if (p1.X == 7) Castling_WhiteKingSide = false;
                if (p1.X == 0) Castling_WhiteQueenSide = false;
            }
            if (movingColor == Pieces.Black && p1.Y == 7)
            {
                if (p1.X == 7) Castling_BlackKingSide = false;
                if (p1.X == 0) Castling_BlackQueenSide = false;
            }
        }

        // --- EN PASSANT STATE ---
        if (movingKind == Pieces.Pawn && Math.Abs(p2.Y - p1.Y) == 2)
            EnPassant = p1.X;
        else
            EnPassant = -1;

        // --- KING CHECK FLAGS ---
        UpdateKingCheckFlags();

        // --- TURN ---
        BoardState = BoardState == BoardState.WhiteTurn
            ? BoardState.BlackTurn
            : BoardState.WhiteTurn;

        return (true, updates);
    }

    private void UpdateKingCheckFlags()
    {
        PiecePosition whiteKing = new(-1, -1);
        PiecePosition blackKing = new(-1, -1);

        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                var p = Board[y, x];
                if (p.GetKind() == Pieces.King)
                {
                    if (p.GetColor() == Pieces.White)
                        whiteKing = new PiecePosition(x, y);

                    else if (p.GetColor() == Pieces.Black)
                        blackKing = new PiecePosition(x, y);
                }
            }
        }

        if (whiteKing.X != -1)
            IsWhiteKingChecked = IsSquareAttacked(Board, whiteKing, Pieces.Black);
        else
            IsWhiteKingChecked = false;

        if (blackKing.X != -1)
            IsBlackKingChecked = IsSquareAttacked(Board, blackKing, Pieces.White);
        else
            IsBlackKingChecked = false;
    }

    private Pieces GetPieceAt(PiecePosition piecePosition)
    {
        return piecePosition.X < 0 || piecePosition.X > 7 || piecePosition.Y < 0 || piecePosition.Y > 7
            ? Pieces.None
            : Board[piecePosition.Y, piecePosition.X];
    }

    public bool CanMove(PiecePosition piecePosition)
    {
        var piece = GetPieceAt(piecePosition);
        if (piece == Pieces.None) return false;

        var validTurn = BoardState switch
        {
            BoardState.WhiteTurn => piece.IsWhite(),
            BoardState.BlackTurn => piece.IsBlack(),
            _ => false,
        };

        if (!validTurn) return false;

        return GetPosibleMovesOfPieceOnPosition(piece, piecePosition).Any();
    }

    private IEnumerable<PiecePosition> GetPosibleMovesOfPieceOnPosition(Pieces piece, PiecePosition currentPosition)
    {
        if (piece.GetColor() == Pieces.None)
            yield break;

        for (short i = 0; i < 8; i++)
        {
            for (short j = 0; j < 8; j++)
            {
                if (currentPosition.Y == i && currentPosition.X == j) continue;

                var newPosition = new PiecePosition(j, i);
                var move = new Move(currentPosition, newPosition);

                if (IsValidMove(move, piece, out _) && !IsKingCheckedValidation(move))
                    yield return newPosition;
            }
        }
    }
}

public record struct Move(PiecePosition From, PiecePosition To);

public record struct PiecePosition(int X, int Y);

[Flags]
public enum Pieces : byte
{
    None = 0x00,
    White = 0x01,
    Black = 0x02,
    Pawn = 0x04,
    Knight = 0x08,
    Bishop = 0x10,
    Rook = 0x20,
    Queen = 0x40,
    King = 0x80,
}

public static class PiecesExtensions
{
    public static bool IsWhite(this Pieces piece) => (piece & Pieces.White) != 0;
    public static bool IsBlack(this Pieces piece) => (piece & Pieces.Black) != 0;

    public static Pieces GetColor(this Pieces piece) => piece & (Pieces.White | Pieces.Black);

    public static Pieces GetKind(this Pieces piece) => piece & ~(Pieces.White | Pieces.Black);
}

public enum BoardState
{
    WhiteTurn,
    BlackTurn,
    Ended,
    WhiteWon,
    BlackWon,
    DrawDeclared,
    Resigned,
    Timeout,
    Stalemate,
    InsufficientMaterial,
    FiftyMoveRule,
    Repetition,
}

public enum BoardUpdateType
{
    Move,
    Remove
}

public record BoardUpdate(
    BoardUpdateType Type,
    PiecePosition From,
    PiecePosition To
);
