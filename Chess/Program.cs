using System;

namespace Chess
{
    class Program
    {
        static void Main(string[] args)
        {
            ChessBoard board = new ChessBoard();
            board.initiate();
            Game game = new Game(board);
            game.play();
        }
    }

    class Game
    {
        bool whiteTurn = true;
        bool gameOn = true;
        bool isDraw = false;
        bool playerResigned = false;
        int fiftyMoveRule = 0;
        ChessPiece pawnMovedTwoSquares = new Empty();
        int[] pawnMovedTwoSquaresLocation = new int[] { -1, -1 };
        ChessBoard board;
        ChessPiece[] whitePieces;
        ChessPiece[] blackPieces;

        ChessBoard[] positionsHistory = { };

        public Game(ChessBoard board)
        {
            this.board = board;
            whitePieces = new ChessPiece[]{ new Rook(true), new Knight(true), new Bishop(true), new Queen(true, new Bishop(true), new Rook(true)), new King(true), new Bishop(true), new Knight(true), new Rook(true),
                                            new Pawn(true), new Pawn(true), new Pawn(true), new Pawn(true), new Pawn(true), new Pawn(true), new Pawn(true), new Pawn(true)};
            blackPieces = new ChessPiece[]{ new Rook(false), new Knight(false), new Bishop(false), new Queen(false, new Bishop(false), new Rook(false)), new King(false), new Bishop(false), new Knight(false), new Rook(false),
                                            new Pawn(false), new Pawn(false), new Pawn(false), new Pawn(false), new Pawn(false), new Pawn(false), new Pawn(false), new Pawn(false)};
        }

        public void play()
        {
            while (gameOn)
            {
                board.print();
                if (isImpossibilityOfCheckmate())
                {
                    gameOn = false;
                    Console.WriteLine("Draw - Impossibility Of Checkmate");
                    break;
                }
                if (isCheckMateOrStalemate())
                {
                    gameOn = false;
                    break;
                }
                if (isFiftyMoveRule())
                {
                    gameOn = false;
                    break;
                }

                makeMove();

                if (isThreefoldRepetition())
                {
                    gameOn = false;
                    break;
                }
            }
        }

        public void makeMove()
        {
            string input;
            do
            {
                input = getInputFromPlayer();
            }
            while (!isDraw && !playerResigned && !isValidInput(input));

            if (!isDraw && !playerResigned && isValidInput(input))
            {
                string[] move = splitUserInputIntoMoves(input);
                string selectedSquareInput = getCoordinatesFromInput(move[0].ToLower());
                int fromRank = int.Parse("" + selectedSquareInput[0]) - 1;
                int fromFile = int.Parse("" + selectedSquareInput[1]);
                ChessPiece selectedPiece = board.getBoard()[fromRank, fromFile];

                if (!(selectedPiece is Empty) && selectedPiece.getIsWhite() == whiteTurn)
                {
                    selectedPiece.getValidMoves(board, fromRank, fromFile);
                    string squareToMoveInput = getCoordinatesFromInput(move[1].ToLower());
                    int toRank = int.Parse("" + squareToMoveInput[0]) - 1;
                    int toFile = int.Parse("" + squareToMoveInput[1]);
                    ChessPiece squareToMove = board.getBoard()[toRank, toFile];

                    //If move is castling
                    if (moveIsCastling(selectedSquareInput, squareToMoveInput, selectedPiece) &&
                        canCastle(selectedPiece, squareToMove, fromRank, fromFile, toRank, toFile))
                    {
                        resetLegalMoves(board);
                        switchTurn();
                        fiftyMoveRule++;
                        pawnMovedTwoSquares = new Empty();
                    }
                    //Check if move is En Passant
                    else if (moveIsEnPassant(selectedPiece, fromRank, fromFile, toRank, toFile))
                    {
                        enPassant(selectedPiece, fromRank, fromFile, toRank, toFile);
                    }
                    else if (squareToMove.getIsValidMove()) //Any other move
                    {
                        checkIfCaptureHappened(toRank, toFile);
                        ChessPiece captured = squareToMove;
                        executeMove(selectedPiece, new Empty(), fromRank, fromFile, toRank, toFile);

                        if (isInCheck(board)) //If the move puts king in check - abort move
                        {
                            executeMove(captured, selectedPiece, fromRank, fromFile, toRank, toFile);
                            Console.WriteLine("Your king can't be in check!");
                        }
                        else
                        {
                            if (selectedPiece.getIsFirstMove())
                                selectedPiece.firstMoveDone();

                            if (selectedPiece is Pawn)
                            {
                                fiftyMoveRule = 0;
                                checkIfPawnMovedTwoSquares(fromRank, fromFile, toRank, toFile); //Check if the pawn moved 2 steps in his first move to enable En Passant capture for oponnent
                                pawnMovedTwoSquaresLocation = new int[] { toRank, toFile };
                                checkPromotion(toRank, toFile); //check if pawn is on the last rank and promote
                            }
                            else
                                pawnMovedTwoSquares = new Empty();

                            addBoardToPositionsHistory(board);
                            resetLegalMoves(board);
                            switchTurn();
                        }
                    }
                    else
                        Console.WriteLine("Invalid move");
                }
                else
                    Console.WriteLine("Please select your own piece");
            }
        }

        public string getInputFromPlayer()
        {
            Console.WriteLine("{0}, please enter your move (for example: \"g4 g5\"):" +
                "\nTo offer a draw - type \"draw\", to resign - type \"resign\"", whiteTurn ? "White" : "Black");

            string input = Console.ReadLine();

            if (input.ToLower() == "draw")
                offerDraw();

            if (input.ToLower() == "resign")
                resignation();

            return input;
        }

        public string getCoordinatesFromInput(string squareInput)
        {
            switch (squareInput[0])
            {
                case 'a':
                    return squareInput[1] + "0";
                case 'b':
                    return squareInput[1] + "1";
                case 'c':
                    return squareInput[1] + "2";
                case 'd':
                    return squareInput[1] + "3";
                case 'e':
                    return squareInput[1] + "4";
                case 'f':
                    return squareInput[1] + "5";
                case 'g':
                    return squareInput[1] + "6";
                case 'h':
                    return squareInput[1] + "7";
                default:
                    return "";
            }
        }

        public string[] splitUserInputIntoMoves(string input)
        {
            //Remove spaces in beginning and in the end
            string[] inputSplit = input.Trim().Split(' ');
            //remove spaces between and sort move credentials in an array
            string[] move = new string[2];
            for (int i = 0, j = 0; i < inputSplit.Length; i++)
            {
                if (inputSplit[i] != "")
                {
                    move[j] = inputSplit[i];
                    j++;
                }
            }
            return move;
        }

        public bool moveIsCastling(string from, string to, ChessPiece selectedPiece)
        {
            return (from == "84" && (to == "86" || to == "82")) ||
                        from == "14" && (to == "16" || to == "12") &&
                        selectedPiece is King && !isInCheck(board);
        }

        public bool moveIsEnPassant(ChessPiece selectedPiece, int fromRank, int fromFile, int toRank, int toFile)
        {
            return selectedPiece is Pawn && (
            fromFile - 1 >= 0 && (board.getBoard()[fromRank, fromFile - 1] == pawnMovedTwoSquares) ||
            fromFile + 1 <= 7 && (board.getBoard()[fromRank, fromFile + 1] == pawnMovedTwoSquares)) &&
            toFile == pawnMovedTwoSquaresLocation[1] &&
            whiteTurn && toFile == pawnMovedTwoSquaresLocation[1] && toRank == pawnMovedTwoSquaresLocation[0] - 1 ||
            (!whiteTurn && toFile == pawnMovedTwoSquaresLocation[1] && toRank == pawnMovedTwoSquaresLocation[0] + 1);
        }

        public void enPassant(ChessPiece selectedPiece, int fromRank, int fromFile, int toRank, int toFile)
        {
            updateCapture(pawnMovedTwoSquares);
            board.getBoard()[toRank, toFile] = selectedPiece;
            board.getBoard()[pawnMovedTwoSquaresLocation[0], pawnMovedTwoSquaresLocation[1]] = new Empty();
            board.getBoard()[fromRank, fromFile] = new Empty();
            resetLegalMoves(board);
            switchTurn();
            fiftyMoveRule = 0;
            pawnMovedTwoSquares = new Empty();
        }

        public void checkIfPawnMovedTwoSquares(int fromRank, int fromFile, int toRank, int toFile)
        {
            if (toRank - fromRank == 2 || toRank - fromRank == -2)
            {
                pawnMovedTwoSquares = board.getBoard()[toRank, toFile];
                pawnMovedTwoSquaresLocation = new int[]{ toRank, toFile };
            }
            else
                pawnMovedTwoSquares = new Empty();
        }

        public void checkIfCaptureHappened(int rank, int file)
        {
            if (!(board.getBoard()[rank, file] is Empty))
            {
                fiftyMoveRule = 0;
                updateCapture(board.getBoard()[rank, file]);
            }
            else
                fiftyMoveRule++;
        }

        public void executeMove(ChessPiece selectedPiece, ChessPiece captured, int fromRank, int fromFile, int toRank, int toFile)
        {
            board.getBoard()[toRank, toFile] = selectedPiece;
            board.getBoard()[fromRank, fromFile] = (captured);
        }

        public void checkPromotion(int rank, int file)
        {
            if (rank == 0 || rank == 7)
            {
                ChessPiece promoted = pawnPromotion();
                board.getBoard()[rank, file] = promoted;
                Console.WriteLine("Your pawn has been promoted to a {0}!", promoted.getName());
            }
        }

        public bool isValidInput(string input)
        {
            if (input.ToLower() == "draw")
            {
                return false;
            }

            string[] inputSplitNoSpaces = input.Trim().Split(' ');
            string inputNoSpaces = string.Join("", inputSplitNoSpaces);
            string validChars = "abcdefghABCDEFGH12345678";

            if (inputNoSpaces.Length != 4)
            {
                Console.WriteLine("Invalid input");
                return false;
            }

            for (int i = 0; i < inputNoSpaces.Length; i++)
            {
                for (int j = 0; j < validChars.Length; j++)
                {
                    if (inputNoSpaces[i] == validChars[j])
                        break;
                    if (j == validChars.Length - 1)
                    {
                        Console.WriteLine("Invalid input");
                        return false;
                    }
                }
            }
            return true;
        }

        public void switchTurn()
        {
            whiteTurn = !whiteTurn;
        }

        public void resetLegalMoves(ChessBoard board)
        {
            for (int i = 0; i < board.getBoard().GetLength(0); i++)
                for (int j = 0; j < board.getBoard().GetLength(1); j++)
                    board.getBoard()[i, j].setIsValidMove(false);
        }

        public void offerDraw()
        {
            switchTurn();
            string response;

            do
            {
                Console.WriteLine("{0} do you accept the draw offer (y / n)? ", whiteTurn ? "White" : "Black");
                response = Console.ReadLine();
            }
            while (response != "Y" && response != "y" && response != "N" && response != "n");

            switch (response)
            {
                case "Y":
                case "y":
                    isDraw = true;
                    Console.WriteLine("Its a draw!");
                    gameOn = false;
                    break;

                default:
                    Console.WriteLine("Draw offer denied!");
                    switchTurn();
                    break;
            }
        }

        public void resignation()
        {
            gameOn = false;
            playerResigned = true;
            Console.WriteLine("{0} resigned, {1} is the winner!", whiteTurn ? "white" : "black", whiteTurn ? "black" : "white");
        }

        public ChessPiece pawnPromotion()
        {
            string input;
            do
            {
                Console.WriteLine("Yor pawn reached the final rank!\nTo which piece would you like to promote it?" +
                    "\nQ for Queen/ R for Rook/ B for Bishop/ N for Knight");

                input = Console.ReadLine().ToLower();

            } while (input != "q" && input.ToLower() != "r" && input.ToLower() != "b" && input.ToLower() != "n");

            switch (input)
            {
                case "q":
                    return new Queen(whiteTurn, new Bishop(whiteTurn), new Rook(whiteTurn));
                case "r":
                    return new Rook(whiteTurn);
                case "b":
                    return new Bishop(whiteTurn);
                case "n":
                    return new Knight(whiteTurn);
                default:
                    return null;
            }
        }

        public bool isInCheck(ChessBoard board)
        {
            resetLegalMoves(board);

            for (int i = 0; i < board.getBoard().GetLength(0); i++)
                for (int j = 0; j < board.getBoard().GetLength(1); j++)
                {
                    if (!(board.getBoard()[i, j] is Empty) && board.getBoard()[i, j].getIsWhite() != whiteTurn)
                    {
                        board.getBoard()[i, j].getValidMoves(board, i, j);
                    }
                }
            ChessPiece playersKingLocation = findKingLocation(board);

            bool isKingInCheck = playersKingLocation.getIsValidMove() ? true : false;
            resetLegalMoves(board);
            return isKingInCheck;
        }

        public ChessPiece findKingLocation(ChessBoard board)
        {
            ChessPiece kingsLocation = new Empty();

            for (int i = 0; i < board.getBoard().GetLength(0); i++)
                for (int j = 0; j < board.getBoard().GetLength(1); j++)
                {
                    if (board.getBoard()[i, j] is King && board.getBoard()[i, j].getIsWhite() == whiteTurn)
                    {
                        kingsLocation = board.getBoard()[i, j];
                    }
                }
            return kingsLocation;
        }

        public bool isCheckMateOrStalemate()
        {
            int validMovesCount = 0;

            for (int i = 0; i < board.getBoard().GetLength(0); i++)
                for (int j = 0; j < board.getBoard().GetLength(1); j++)
                {
                    if (!(board.getBoard()[i, j] is Empty) && board.getBoard()[i, j].getIsWhite() == whiteTurn)
                    {
                        board.getBoard()[i, j].getValidMoves(board, i, j);
                        validateNoCheck(i, j);
                        resetLegalMoves(board);
                    }
                }

            void validateNoCheck(int a, int b)
            {
                for (int i = 0; i < board.getBoard().GetLength(0); i++)
                    for (int j = 0; j < board.getBoard().GetLength(1); j++)
                    {
                        if (board.getBoard()[i, j].getIsValidMove())
                        {
                            ChessPiece pieceToCheck = board.getBoard()[a, b];
                            //ChessBoard boardCopy = new ChessBoard(board);
                            if (!previewIfKingInCheckAfterMove(board, pieceToCheck, a, b, i, j))
                                validMovesCount++;
                            board.getBoard()[a, b].getValidMoves(board, a, b);
                        }
                    }
            }

            bool hasValidMoves = validMovesCount == 0 ? false : true;

            if (isInCheck(board) && !hasValidMoves)
            {
                Console.WriteLine("Checkmate! {0} won!", !whiteTurn ? "white" : "black");
                return true;
            }
            else if (!hasValidMoves)
            {
                Console.WriteLine("Draw by stalemate!");
                isDraw = true;
                return true;
            }
            else
                return false;
        }

        public bool previewIfKingInCheckAfterMove(ChessBoard board, ChessPiece pieceToCheck, int rank, int file, int newRank, int newFile)
        {
            ChessPiece canBeCaptured = board.getBoard()[newRank, newFile];

            board.getBoard()[newRank, newFile] = pieceToCheck;
            board.getBoard()[rank, file] = new Empty();

            bool movePutsKingIncheck = isInCheck(board);

            board.getBoard()[newRank, newFile] = canBeCaptured;
            board.getBoard()[rank, file] = pieceToCheck;

            return movePutsKingIncheck;
        }

        public bool canCastle(ChessPiece selectedSquare, ChessPiece squareToMove, int fromRank, int fromFile, int toRank, int toFile)
        {
            bool castlingDone = false;
            //ChessBoard boardCopy = new ChessBoard(board);
            //int file = squareToMove.getFile(); //6 for king's side, 2 for queen's side
            // rank = squareToMove.getRank(); //7 for white, 0 for black
            // Castle king's side
            if (toFile == 6)
            {
                if (!board.isOccupied(fromRank, 5) && !isUnderAttack(board, fromRank, 5) &&
                    !board.isOccupied(toRank, toFile) && !isUnderAttack(board, toRank, toFile) &&
                    board.getBoard()[fromRank, 7] is Rook)
                {
                    if (selectedSquare.getIsFirstMove() && board.getBoard()[fromRank, 7].getIsFirstMove())
                    {
                        board.getBoard()[fromRank, fromFile] = new Empty();
                        board.getBoard()[fromRank, 5] = board.getBoard()[fromRank, 7];
                        board.getBoard()[toRank, toFile] = selectedSquare;
                        board.getBoard()[fromRank, 7] = new Empty();
                        castlingDone = true;
                    }
                }
            }
            //Castle Queen's side
            if (toFile == 2)
            {
                if (!board.isOccupied(fromRank, 1) && !isUnderAttack(board, fromRank, 1) &&
                    !board.isOccupied(toRank, toFile) && !isUnderAttack(board, toRank, toFile) &&
                    !board.isOccupied(fromRank, 3) && !isUnderAttack(board, fromRank, 3) &&
                    board.getBoard()[fromRank, 0] is Rook)
                {
                    if (selectedSquare.getIsFirstMove() && board.getBoard()[fromRank, 0].getIsFirstMove())
                    {
                        board.getBoard()[fromRank, fromFile] = new Empty();
                        board.getBoard()[fromRank, 3] = board.getBoard()[fromRank, 0];
                        board.getBoard()[toRank, toFile] = selectedSquare;
                        board.getBoard()[fromRank, 1] = new Empty();
                        board.getBoard()[fromRank, 0] = new Empty();
                        castlingDone = true;
                    }
                }
            }
            return castlingDone;
        }

        public bool isUnderAttack(ChessBoard board, int rank, int file)
        {
            resetLegalMoves(board);
            ChessPiece current = board.getBoard()[rank, file];

            for (int i = 0; i < board.getBoard().GetLength(0); i++)
                for (int j = 0; j < board.getBoard().GetLength(1); j++)
                {
                    if (board.isOccupied(i, j) && board.getBoard()[i, j].getIsWhite() != whiteTurn)
                    {
                        board.getBoard()[i, j].getValidMoves(board, i, j);
                    }
                }

            bool isItUnderAttack = current.getIsValidMove() ? true : false;
            resetLegalMoves(board);
            return isItUnderAttack;
        }

        public bool isFiftyMoveRule()
        {
            if (fiftyMoveRule == 50)
            {
                gameOn = false;
                isDraw = true;
                Console.WriteLine("Draw by 50 move rule!");
                return true;
            }
            return false;
        }

        public bool isThreefoldRepetition()
        {
            for (int i = 0; i < positionsHistory.Length; i++)
            {
                int timesPositionRepeated = 0;
                //look for identical positions
                for (int j = 0; j < positionsHistory.Length; j++)
                {
                    if (positionsHistory[i].Equals(positionsHistory[j]))
                        timesPositionRepeated++;

                    if (timesPositionRepeated == 3) //3 identical positions
                    {
                        isDraw = true;
                        Console.WriteLine("Draw by Threefold Repetition!");
                        return true;
                    }
                }
            }
            return false;
        }

        public void addBoardToPositionsHistory(ChessBoard position)
        {
            //Create new array of chess boards
            ChessBoard[] updatedPositionsHistory = new ChessBoard[positionsHistory.Length + 1];
            // copy positions history to the new array
            for (int i = 0; i < positionsHistory.Length; i++)
                updatedPositionsHistory[i] = new ChessBoard(positionsHistory[i]);
            //Add the current position to the new array
            updatedPositionsHistory[updatedPositionsHistory.Length - 1] = new ChessBoard(position);
            positionsHistory = updatedPositionsHistory;
        }

        public void updateCapture(ChessPiece piece)
        {
            if (whiteTurn)
            {
                for (int i = 0; i < blackPieces.Length; i++)
                    if (!(blackPieces[i] is Empty) && blackPieces[i].ToString() == piece.ToString())
                    {
                        blackPieces[i] = new Empty();
                        break;
                    }
            }
            else
            {
                for (int i = 0; i < whitePieces.Length; i++)
                    if (!(whitePieces[i] is Empty) && whitePieces[i].ToString() == piece.ToString())
                    {
                        whitePieces[i] = new Empty();
                        break;
                    }
            }
        }

        public bool isImpossibilityOfCheckmate()
        {
            ChessPiece[] whiteLoneKing = new ChessPiece[]{ new Empty(), new Empty(), new Empty(), new Empty(), new King(true), new Empty(), new Empty(), new Empty(),
                                                         new Empty(), new Empty(), new Empty(), new Empty(), new Empty(), new Empty(), new Empty(), new Empty()};
            ChessPiece[] blackLoneKing = new ChessPiece[]{ new Empty(), new Empty(), new Empty(), new Empty(), new King(false), new Empty(), new Empty(), new Empty(),
                                                         new Empty(), new Empty(), new Empty(), new Empty(), new Empty(), new Empty(), new Empty(), new Empty()};
            ChessPiece[] whiteKingAndBishop = new ChessPiece[]{ new Empty(), new Empty(), new Empty(), new Empty(), new King(true), new Bishop(true), new Empty(), new Empty(),
                                                         new Empty(), new Empty(), new Empty(), new Empty(), new Empty(), new Empty(), new Empty(), new Empty()};
            ChessPiece[] blackKingAndBishop = new ChessPiece[]{ new Empty(), new Empty(), new Empty(), new Empty(), new King(false), new Bishop(false), new Empty(), new Empty(),
                                                         new Empty(), new Empty(), new Empty(), new Empty(), new Empty(), new Empty(), new Empty(), new Empty()};
            ChessPiece[] whiteKingAndKnight = new ChessPiece[]{ new Empty(), new Empty(), new Empty(), new Empty(), new King(true), new Empty(), new Knight(true), new Empty(),
                                                         new Empty(), new Empty(), new Empty(), new Empty(), new Empty(), new Empty(), new Empty(), new Empty()};
            ChessPiece[] blackKingAndKnight = new ChessPiece[]{ new Empty(), new Empty(), new Empty(), new Empty(), new King(false), new Empty(), new Knight(false), new Empty(),
                                                         new Empty(), new Empty(), new Empty(), new Empty(), new Empty(), new Empty(), new Empty(), new Empty()};

            if (equals(whitePieces, whiteLoneKing) && equals(blackPieces, blackLoneKing))
                return true;
            if (equals(whitePieces, whiteLoneKing) && equals(blackPieces, blackKingAndBishop))
                return true;
            if (equals(whitePieces, whiteLoneKing) && equals(blackPieces, blackKingAndKnight))
                return true;
            if (equals(whitePieces, whiteKingAndBishop) && equals(blackPieces, blackLoneKing))
                return true;
            if (equals(whitePieces, whiteKingAndKnight) && equals(blackPieces, blackLoneKing))
                return true;
            return false;
        }

        public bool equals(ChessPiece[] current, ChessPiece[] other)
        {
            for (int i = 0; i < current.Length; i++)
            {
                if (!(current[i] is Empty) && !(other[i] is Empty))
                {
                    if (current[i].ToString() != other[i].ToString())
                        return false;
                }
                if (current[i] is Empty && !(other[i] is Empty))
                    return false;
                if (!(current[i] is Empty) && other[i] is Empty)
                    return false;
            }
            return true;
        }
    }

    class ChessBoard
    {
        ChessPiece[,] board = new ChessPiece[8, 8];

        public ChessPiece[,] getBoard() { return board; }

        public ChessBoard() { }

        public ChessBoard(ChessBoard other)
        {
            for (int rank = 0; rank < 8; rank++)
                for (int file = 0; file < 8; file++)
                    board[rank, file] = new ChessPiece(other.getBoard()[rank, file]);
        }

        public void print()
        {
            for (int i = 0; i < board.GetLength(0); i++)
            {
                Console.Write((i + 1) + " ");
                for (int j = 0; j < board.GetLength(1); j++)
                    Console.Write(board[i, j] + " ");
                Console.WriteLine();
            }
            Console.WriteLine("  a  b  c  d  e  f  g  h");
        }

        public void initiate()
        {
            board[0, 0] = new Rook(false);
            board[0, 1] = new Knight(false);
            board[0, 2] = new Bishop(false);
            board[0, 3] = new Queen(false, new Bishop(false), new Rook(false));
            board[0, 4] = new King(false);
            board[0, 5] = new Bishop(false);
            board[0, 6] = new Knight(false);
            board[0, 7] = new Rook(false);

            for (int i = 0; i < 8; i++)
                board[1, i] = new Pawn(false);

            for (int i = 2; i < 6; i++)
                for (int j = 0; j < 8; j++)
                    board[i, j] = new Empty();

            for (int i = 0; i < 8; i++)
                board[6, i] = new Pawn(true);

            board[7, 0] = new Rook(true);
            board[7, 1] = new Knight(true);
            board[7, 2] = new Bishop(true);
            board[7, 3] = new Queen(true, new Bishop(true), new Rook(true));
            board[7, 4] = new King(true);
            board[7, 5] = new Bishop(true);
            board[7, 6] = new Knight(true);
            board[7, 7] = new Rook(true);
        }

        public static bool isOutOfBoard(int[] credentials)
        {
            bool result = (credentials[0] < 0 || credentials[0] > 7 || credentials[1] < 0 || credentials[1] > 7) ? true : false;
            return result;
        }

        public static bool checkIfOccupiedByOwnPiece(ChessBoard board, int[] credentials, bool isWhite)
        {
            ChessPiece piece = board.board[credentials[0], credentials[1]];
            if (piece is Empty)
                return false;

            bool result = !(piece is Empty) && piece.getIsWhite() == isWhite ? true : false;
            return result;
        }

        public bool isOccupied(int rank, int file)
        {
            if (board[rank, file] is Empty)
                return false;
            else
                return true;
        }

        public override bool Equals(object obj)
        {
            ChessBoard other = (ChessBoard)obj;
            for (int rank = 0; rank < 8; rank++)
                for (int file = 0; file < 8; file++)
                {
                    if (!this.getBoard()[rank, file].Equals(other.getBoard()[rank, file]))
                        return false;
                }
            return true;
        }
    }

    class ChessPiece
    {
        string name;
        bool isWhite;
        bool isFirstMove = true;
        bool isValidMove;

        public string getName() { return name; }
        public bool getIsWhite() { return isWhite; }
        public bool getIsValidMove() { return isValidMove; }
        public bool getIsFirstMove() { return isFirstMove; }
        public void setName(string name) { this.name = name; }
        public void setIsWhite(bool isWhite) { this.isWhite = isWhite; }
        public void setIsValidMove(bool isValidMove) { this.isValidMove = isValidMove; }
        public void firstMoveDone() { isFirstMove = false; }

        public ChessPiece() { }

        public ChessPiece(bool isWhite)
        {
            this.isWhite = isWhite;
        }

        public ChessPiece(ChessPiece other)
        {
            name = other.name;
            isWhite = other.isWhite;
            isFirstMove = other.isFirstMove;
            isValidMove = other.getIsValidMove();
        }

        public override string ToString()
        {
            if (this is Empty)
                return "  ";
            return "" + (isWhite ? "W" : "B") + name;
        }

        public override bool Equals(object obj)
        {
            ChessPiece other = (ChessPiece)obj;
            if (this.ToString() == other.ToString())
                return true;
            else
                return false;
        }

        public virtual void getValidMoves(ChessBoard board, int rank, int file) { }
    }

    class Empty : ChessPiece
    {
        public override string ToString() { return "- "; }
    }

    class Pawn : ChessPiece
    {
        public Pawn(bool isWhite) : base(isWhite) { setName("p"); }

        public override void getValidMoves(ChessBoard board, int rank, int file)
        {
            if (getIsWhite()) //Moving direction according to pawn's color
            {
                int[][] possibleMoves = new int[3][];

                possibleMoves[0] = new int[] { rank - 1, file };
                possibleMoves[1] = new int[] { rank - 1, file - 1 };
                possibleMoves[2] = new int[] { rank - 1, file + 1 };

                //Move 1 square forward
                if (!board.isOccupied(possibleMoves[0][0], possibleMoves[0][1]))
                    board.getBoard()[possibleMoves[0][0], possibleMoves[0][1]].setIsValidMove(true);

                //Capture diagonally to the left
                if (!ChessBoard.isOutOfBoard(possibleMoves[1]) &&
                    board.isOccupied(rank - 1, file - 1) &&
                    board.getBoard()[rank - 1, file - 1].getIsWhite() != getIsWhite())
                    board.getBoard()[rank - 1, file - 1].setIsValidMove(true);

                //Capture diagonally to the right
                if (!ChessBoard.isOutOfBoard(possibleMoves[2]) &&
                    board.isOccupied(rank - 1, file + 1) &&
                    board.getBoard()[rank - 1, file + 1].getIsWhite() != getIsWhite())
                    board.getBoard()[rank - 1, file + 1].setIsValidMove(true);

                // Move 2 squares forward on first move
                if (getIsFirstMove() &&
                    !board.isOccupied(rank - 2, file))
                    board.getBoard()[rank - 2, file].setIsValidMove(true);
            }
            else
            {
                int[][] possibleMoves = new int[3][];

                possibleMoves[0] = new int[] { rank + 1, file };
                possibleMoves[1] = new int[] { rank + 1, file - 1 };
                possibleMoves[2] = new int[] { rank + 1, file + 1 };

                //Move 1 square forward
                if (!(board.isOccupied(rank + 1, file)))
                    board.getBoard()[rank + 1, file].setIsValidMove(true);

                //Capture diagonally to the right
                if (!ChessBoard.isOutOfBoard(possibleMoves[1]) &&
                    board.isOccupied(rank + 1, file - 1) &&
                    board.getBoard()[rank + 1, file - 1].getIsWhite() != getIsWhite())
                    board.getBoard()[rank + 1, file - 1].setIsValidMove(true);

                //Capture diagonally to the left                                      
                if (!ChessBoard.isOutOfBoard(possibleMoves[2]) &&
                    board.isOccupied(rank + 1, file + 1) &&
                    board.getBoard()[rank + 1, file + 1].getIsWhite() != getIsWhite())
                    board.getBoard()[rank + 1, file + 1].setIsValidMove(true);

                // Move 2 squares forward on first move                                      
                if (getIsFirstMove() &&
                    !board.isOccupied(rank + 2, file))
                    board.getBoard()[rank + 2, file].setIsValidMove(true);
            }
        }
    }

    class Knight : ChessPiece
    {
        public Knight(bool isWhite) : base(isWhite) { setName("N"); }

        public override void getValidMoves(ChessBoard board, int rank, int file)
        {
            int[][] possibleMoves = new int[8][]; //Max 8 possible moves

            possibleMoves[0] = new int[] { rank + 2, file + 1 };
            possibleMoves[1] = new int[] { rank + 2, file - 1 };
            possibleMoves[2] = new int[] { rank + 1, file + 2 };
            possibleMoves[3] = new int[] { rank - 1, file + 2 };
            possibleMoves[4] = new int[] { rank - 2, file + 1 };
            possibleMoves[5] = new int[] { rank - 2, file - 1 };
            possibleMoves[6] = new int[] { rank + 1, file - 2 };
            possibleMoves[7] = new int[] { rank - 1, file - 2 };

            //Validate each move individually
            for (int i = 0; i < possibleMoves.Length; i++)
                if (!ChessBoard.isOutOfBoard(possibleMoves[i]) && (!ChessBoard.checkIfOccupiedByOwnPiece(board, possibleMoves[i], getIsWhite())))
                    board.getBoard()[possibleMoves[i][0], possibleMoves[i][1]].setIsValidMove(true);
        }
    }

    class Bishop : ChessPiece
    {
        public Bishop(bool isWhite) : base(isWhite) { setName("B"); }

        //Validate moves of each diagonal direction separately 
        public override void getValidMoves(ChessBoard board, int rank, int file)
        {
            for (int i = 1; i < 8; i++)
            {
                int[] possibleMove = { rank - i, file - i };

                if (validateDiagonalMoves(possibleMove, board) == false)
                    break;
            }
            for (int i = 1; i < 8; i++)
            {
                int[] possibleMove = { rank + i, file + i };

                if (validateDiagonalMoves(possibleMove, board) == false)
                    break;
            }
            for (int i = 1; i < 8; i++)
            {
                int[] possibleMove = { rank + i, file - i };

                if (validateDiagonalMoves(possibleMove, board) == false)
                    break;
            }
            for (int i = 1; i < 8; i++)
            {
                int[] possibleMove = { rank - i, file + i };

                if (validateDiagonalMoves(possibleMove, board) == false)
                    break;
            }
        }

        bool validateDiagonalMoves(int[] possibleMove, ChessBoard board)
        {
            //The square is within board limits and is empty
            //Continue checking next aquare in this diagonal
            if (!ChessBoard.isOutOfBoard(possibleMove) && !(board.isOccupied(possibleMove[0], possibleMove[1])))
            {
                board.getBoard()[possibleMove[0], possibleMove[1]].setIsValidMove(true);
                return true;
            }
            //The square is within board limits and is occupied by opponent's piece
            //Validate this square and stop cheching this diagonal
            else if (!ChessBoard.isOutOfBoard(possibleMove) && !ChessBoard.checkIfOccupiedByOwnPiece(board, possibleMove, getIsWhite()))
            {
                board.getBoard()[possibleMove[0], possibleMove[1]].setIsValidMove(true);
                return false;
            }
            //The square is out of board limits or is occupied by player's piece
            //Stop checking this diagonal
            else
                return false;
        }
    }

    class Rook : ChessPiece
    {
        public Rook(bool isWhite) : base(isWhite) { setName("R"); }

        public override void getValidMoves(ChessBoard board, int rank, int file)
        {
            //Validate moves of each line direction separately 
            for (int i = 1; i < 8; i++)
            {
                int[] possibleMove = { rank, file - i };

                if (validateLineMoves(possibleMove, board) == false)
                    break;
            }
            for (int i = 1; i < 8; i++)
            {
                int[] possibleMove = { rank, file + i };

                if (validateLineMoves(possibleMove, board) == false)
                    break;
            }
            for (int i = 1; i < 8; i++)
            {
                int[] possibleMove = { rank + i, file };

                if (validateLineMoves(possibleMove, board) == false)
                    break;
            }
            for (int i = 1; i < 8; i++)
            {
                int[] possibleMove = { rank - i, file };

                if (validateLineMoves(possibleMove, board) == false)
                    break;
            }
        }

        bool validateLineMoves(int[] possibleMove, ChessBoard board)
        {
            if (!ChessBoard.isOutOfBoard(possibleMove) && !board.isOccupied(possibleMove[0], possibleMove[1]))
            {
                board.getBoard()[possibleMove[0], possibleMove[1]].setIsValidMove(true);
                return true;
            }
            else if (!ChessBoard.isOutOfBoard(possibleMove) && (!ChessBoard.checkIfOccupiedByOwnPiece(board, possibleMove, getIsWhite())))
            {
                board.getBoard()[possibleMove[0], possibleMove[1]].setIsValidMove(true);
                return false;
            }
            else
                return false;
        }
    }

    class Queen : ChessPiece
    {
        Bishop bishop;
        Rook rook;

        public Queen(bool isWhite, Bishop bishop, Rook rook) : base(isWhite)
        {
            setName("Q");
            this.bishop = bishop;
            this.rook = rook;
        }
        //Queen's moves are combined of Rook and Bishop moves
        public override void getValidMoves(ChessBoard board, int rank, int file)
        {
            bishop.getValidMoves(board, rank, file);
            rook.getValidMoves(board, rank, file);
        }
    }

    class King : ChessPiece
    {
        public King(bool isWhite) : base(isWhite) { setName("K"); }

        public override void getValidMoves(ChessBoard board, int rank, int file)
        {
            int[][] possibleMoves = new int[8][]; //Max 8 possible moves

            //Can move one square in any direction
            possibleMoves[0] = new int[] { rank, file + 1 };
            possibleMoves[1] = new int[] { rank - 1, file + 1 };
            possibleMoves[2] = new int[] { rank - 1, file };
            possibleMoves[3] = new int[] { rank - 1, file - 1 };
            possibleMoves[4] = new int[] { rank, file - 1 };
            possibleMoves[5] = new int[] { rank + 1, file - 1 };
            possibleMoves[6] = new int[] { rank + 1, file };
            possibleMoves[7] = new int[] { rank + 1, file + 1 };

            for (int i = 0; i < possibleMoves.Length; i++)
            {
                if (!ChessBoard.isOutOfBoard(possibleMoves[i]) && (!ChessBoard.checkIfOccupiedByOwnPiece(board, possibleMoves[i], getIsWhite())))
                {
                    board.getBoard()[possibleMoves[i][0], possibleMoves[i][1]].setIsValidMove(true);
                }
            }
        }
    }
}
