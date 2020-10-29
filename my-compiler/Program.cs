using System;
using System.Collections.Generic;
using System.Linq;

namespace my_compiler
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                Console.Write(">");
                var line = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(line))
                    return;

                var parser = new Parser(line);
                var syntaxTree = parser.Parse();

                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.DarkGray;
                PrettyPrint(syntaxTree.Root);
                Console.ForegroundColor = color;

                // diagnostics means errors
                if (!syntaxTree.Diagnostics.Any())
                {
                    var e = new Evaluator(syntaxTree.Root);
                    var result = e.Evaluate();
                    Console.WriteLine(result);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    foreach (var diagnostic in syntaxTree.Diagnostics)
                    {
                        Console.WriteLine(diagnostic);
                    }
                    Console.ForegroundColor = color;
                }
            }
        }

        // this method is for printing a very beautiful parse tree
        static void PrettyPrint(SyntaxNode node, string indent = "", bool isLast = true)
        {
            var marker = isLast ? "└──" : "├──";

            Console.Write(indent);
            Console.Write(marker);
            Console.Write(node.Kind);

            if (node is SyntaxToken t && t.Value != null)
            {
                Console.Write(" ");
                Console.Write(t.Value);
            }

            Console.WriteLine();
            indent += isLast ? "   " : "│    ";

            var lastChild = node.GetChildren().LastOrDefault();

            foreach (var child in node.GetChildren())
            {
                PrettyPrint(child, indent, child == lastChild);
            }
        }

        // an enum is a special class that represents a group of unchangeable/readonly constants
        enum SyntaxKind
        {
            NumberToken,
            WhiteSpaceToken,
            PlusToken,
            MinusToken,
            StarToken,
            SlashToken,
            OpenParanthesisToken,
            CloseParanthesisToken,
            BadToken,
            EndOfFileToken,
            NumberExpression,
            BinaryExpression
        }

        // SyntaxNode is an abstract class defined at line 215
        //SyntaxToken represents a word in our language
        class SyntaxToken : SyntaxNode
        {
            public SyntaxToken(SyntaxKind kind, int position, string text, object value)
            {
                Kind = kind;
                Position = position;
                Text = text;
                Value = value;
            }

            // properties
            public override SyntaxKind Kind { get; }
            public int Position { get; }
            public string Text { get; }
            public object Value { get; }

            public override IEnumerable<SyntaxNode> GetChildren()
            {
                return Enumerable.Empty<SyntaxNode>();
            }
        }

        // creating a class for lexer. The lexer produces tokens
        class Lexer
        {
            private readonly string _text; //_ is naming convention
            private int _position;

            // for the list of errors we've created a list
            private List<string> _diagnostics = new List<string>();

            // constructor of the class Lexer
            public Lexer(string text)
            {
                _text = text;
            }

            // arrow is same as JS's arrow function
            // made it public and it returns the list of errors
            public IEnumerable<string> Diagnostics => _diagnostics;

            private char Current // tracks the current character and returns it
            {
                get
                {
                    if (_position >= _text.Length)
                    {
                        return '\0'; //zero terminator (conventional)
                    }
                    return _text[_position];
                }
            }

            private void Next()
            {
                _position++;
            }

            // SyntaxToken is basically what represents a word in our language
            public SyntaxToken NextToken()
            {
                //will be detecting numbers, +,-,*,/, (,),and whitespace.

                if (_position >= _text.Length)
                {
                    return new SyntaxToken(SyntaxKind.EndOfFileToken, _position, "\0", null);
                }

                if (char.IsDigit(Current))
                {
                    var start = _position;

                    while (char.IsDigit(Current))
                        Next();

                    var length = _position - start;
                    var text = _text.Substring(start, length);

                    if (!int.TryParse(text, out var value))
                    {
                        _diagnostics.Add($"The number {_text} is not a valid Int");
                    }
                    return new SyntaxToken(SyntaxKind.NumberToken, start, text, value);
                }

                if (char.IsWhiteSpace(Current))
                {
                    var start = _position;

                    while (char.IsWhiteSpace(Current))
                        Next();

                    var length = _position - start;
                    var text = _text.Substring(start, length);
                    int.TryParse(text, out var value);
                    return new SyntaxToken(SyntaxKind.WhiteSpaceToken, start, text, null);
                }
                //handling plus
                if (Current == '+')
                {
                    return new SyntaxToken(SyntaxKind.PlusToken, _position++, "+", null);
                }
                //handling minus
                else if (Current == '-')
                {
                    return new SyntaxToken(SyntaxKind.MinusToken, _position++, "-", null);
                }
                //handling *
                else if (Current == '*')
                {
                    return new SyntaxToken(SyntaxKind.StarToken, _position++, "*", null);
                }
                // handling /
                else if (Current == '/')
                {
                    return new SyntaxToken(SyntaxKind.SlashToken, _position++, "/", null);
                }
                // handling (
                else if (Current == '(')
                {
                    return new SyntaxToken(SyntaxKind.OpenParanthesisToken, _position++, "(", null);
                }
                //handling ) 
                else if (Current == ')')
                {
                    return new SyntaxToken(SyntaxKind.CloseParanthesisToken, _position++, ")", null);
                }

                _diagnostics.Add($"Error: Bad character input->  `{Current}`");
                return new SyntaxToken(SyntaxKind.BadToken, _position++, _text.Substring(_position - 1, 1), null);
            }
        }

        abstract class SyntaxNode
        //it's abstract because it's just the base type for all syntax nodes
        {
            public abstract SyntaxKind Kind { get; }

            public abstract IEnumerable<SyntaxNode> GetChildren();
        }
        abstract class ExpressionSyntax : SyntaxNode
        {

        }

        sealed class NumberExpressionSyntax : ExpressionSyntax
        {
            public NumberExpressionSyntax(SyntaxToken numberToken)
            {
                NumberToken = numberToken;
            }

            public override SyntaxKind Kind => SyntaxKind.NumberExpression;
            public SyntaxToken NumberToken { get; }

            public override IEnumerable<SyntaxNode> GetChildren()
            {
                //yield is a very convenient way to write stateful iterators. can also create an arry instead
                yield return NumberToken;
            }
        }

        sealed class BinaryExpressionSyntax : ExpressionSyntax
        {
            public BinaryExpressionSyntax(ExpressionSyntax left, SyntaxToken operatorToken, ExpressionSyntax right)
            {
                Left = left;
                OperatorToken = operatorToken;
                Right = right;
            }
            public override SyntaxKind Kind => SyntaxKind.BinaryExpression;
            public ExpressionSyntax Left { get; }
            public SyntaxToken OperatorToken { get; }
            public ExpressionSyntax Right { get; }

            public override IEnumerable<SyntaxNode> GetChildren()
            {
                yield return Left;
                yield return OperatorToken;
                yield return Right;
            }
        }

        sealed class SyntaxTree
        {
            public SyntaxTree(IEnumerable<string> diagnostics, ExpressionSyntax root, SyntaxToken endOfFileToken)
            {
                Diagnostics = diagnostics.ToArray();
                Root = root;
                EndOfFileToken = endOfFileToken;
            }
            public IReadOnlyList<string> Diagnostics { get; }
            public ExpressionSyntax Root { get; }
            public SyntaxToken EndOfFileToken { get; }
        }
        // creating a class for parser. the parser produces sentences (actual trees) 
        class Parser
        {
            private readonly SyntaxToken[] _tokens;
            private List<string> _diagnostics = new List<string>();
            private int _position;

            public Parser(string text)
            {
                var tokens = new List<SyntaxToken>();
                var lexer = new Lexer(text);
                SyntaxToken token;
                do
                {
                    token = lexer.NextToken();
                    if (token.Kind != SyntaxKind.WhiteSpaceToken && token.Kind != SyntaxKind.BadToken)
                    {
                        tokens.Add(token);
                    }
                } while (token.Kind != SyntaxKind.EndOfFileToken);
                _tokens = tokens.ToArray();
                // So that we don't forget what the lexer reported
                _diagnostics.AddRange(lexer.Diagnostics);
            }

            // to make it public and return the list of diagnostics
            public IEnumerable<string> Diagnostics => _diagnostics;

            private SyntaxToken Peek(int offset)
            {
                var index = _position + offset;
                if (index >= _tokens.Length)
                {
                    return _tokens[_tokens.Length - 1];
                }
                return _tokens[index];
            }

            private SyntaxToken Current => Peek(0);

            private SyntaxToken NextToken()
            {
                var current = Current;
                _position++;
                return current;
            }

            private SyntaxToken Match(SyntaxKind kind)
            {
                if (Current.Kind == kind)
                {
                    return NextToken();
                }
                _diagnostics.Add($"Error: Unexpected token <{Current.Kind}>, expected <{kind}>");
                return new SyntaxToken(kind, Current.Position, null, null);
            }

            public SyntaxTree Parse()
            {
                var expression = ParseTerm();
                var endOfFileToken = Match(SyntaxKind.EndOfFileToken);
                return new SyntaxTree(_diagnostics, expression, endOfFileToken);
            }

            private ExpressionSyntax ParseTerm()
            {
                var left = ParseFactor();

                while (Current.Kind == SyntaxKind.PlusToken || Current.Kind == SyntaxKind.MinusToken)
                {
                    var operatorToken = NextToken();
                    var right = ParseFactor();
                    left = new BinaryExpressionSyntax(left, operatorToken, right);
                }
                return left;
            }

            private ExpressionSyntax ParseFactor()
            {
                var left = ParsePrimaryExpression();

                while (Current.Kind == SyntaxKind.StarToken || Current.Kind == SyntaxKind.SlashToken)
                {
                    var operatorToken = NextToken();
                    var right = ParsePrimaryExpression();
                    left = new BinaryExpressionSyntax(left, operatorToken, right);
                }
                return left;
            }

            private ExpressionSyntax ParsePrimaryExpression()
            {
                var numberToken = Match(SyntaxKind.NumberToken);
                return new NumberExpressionSyntax(numberToken);
            }
        }

        class Evaluator
        {
            private readonly ExpressionSyntax _root;
            public Evaluator(ExpressionSyntax root)
            {
                this._root = root;
            }
            public int Evaluate()
            {
                return EvaluateExpression(_root);
            }
            private int EvaluateExpression(ExpressionSyntax node)
            {
                if (node is NumberExpressionSyntax n)
                {
                    return (int)n.NumberToken.Value;
                }

                if (node is BinaryExpressionSyntax b)
                {
                    var left = EvaluateExpression(b.Left);
                    var right = EvaluateExpression(b.Right);

                    if (b.OperatorToken.Kind == SyntaxKind.PlusToken)
                    {
                        return left + right;
                    }
                    else if (b.OperatorToken.Kind == SyntaxKind.MinusToken)
                    {
                        return left - right;
                    }
                    else if (b.OperatorToken.Kind == SyntaxKind.StarToken)
                    {
                        return left * right;
                    }
                    else if (b.OperatorToken.Kind == SyntaxKind.SlashToken)
                    {
                        return left / right;
                    }
                    else
                        throw new Exception($"Unexpected binary operator {b.OperatorToken.Kind}");
                }
                throw new Exception($"Unexpected node {node.Kind}");
            }
        }
    }
}
