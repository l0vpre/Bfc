using System.Text;
using System.Diagnostics;

namespace Brainfuck;
internal class Program
{
    public static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            System.Console.WriteLine("bfc [-c <input> [cflags]| -i input]");
            return 1;
        }

        string input = File.ReadAllText(args[1]);
        string outputCFile = $"{args[1]}.c";
        switch (args[0])
        {
            case "-c":
                File.WriteAllText(outputCFile, Compile(input));
                string gccArgumets = string.Join(" ", args.Skip(2));
                RunCommand($"gcc {outputCFile} {gccArgumets}");
                //использовать другой компилятор на шиндовс
                break;
            case "-i":
                Interpret(input);
                break;
            default:
                System.Console.WriteLine($"Unrecognized option: {args[0]}");
                break;
        }
        return 0;
    }

    static public string Compile(string input)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("""
            #include <stdio.h>
            #include <stdlib.h>

            typedef struct
            {
                unsigned char *items;
                size_t capacity;
            } Data;

            Data data = {0};
            ssize_t current_index;

            void expand()
            {
                size_t new_capacity = data.capacity * 2;
                data.items = realloc(data.items, new_capacity);
                for (size_t i = data.capacity; i < new_capacity; i++)
                {
                    data.items[i] = 0;
                }
                data.capacity = new_capacity;
            }

            void init_data()
            {
                data.items = malloc(30000);
                data.capacity = 30000;
                for (size_t i = 0; i < data.capacity; i++)
                {
                    data.items[i] = 0;
                }
            }

            void inc()
            {
                current_index++;
                if (current_index >= data.capacity)
                {
                    expand();
                }
            }

            void dec()
            {
                current_index--;
                if (current_index < 0)
                {
                    fprintf(stderr, "Runtime Error: Data index was less than zero");
                    exit(1);
                }
            }

            int main()
            {
                init_data();

            """);

        int currentIndent = 1;
        var appendIndentedLine = (string s) =>
        {
            for (int i = 0; i < currentIndent; i++)
            {
                sb.Append("    ");
            }
            sb.Append(s);
            sb.Append("\n");
        };

        foreach (char op in input)
        {
            switch (op)
            {
                case '+':
                    appendIndentedLine("data.items[current_index]++;");
                    break;
                case '-':
                    appendIndentedLine("data.items[current_index]--;");
                    break;
                case '>':
                    appendIndentedLine("inc();");
                    break;
                case '<':
                    appendIndentedLine("dec();");
                    break;
                case '.':
                    appendIndentedLine("putchar(data.items[current_index]);");
                    break;
                case ',':
                    appendIndentedLine("data.items[current_index] = getchar();");
                    break;
                case '[':
                    appendIndentedLine("while(data.items[current_index] > 0)");
                    appendIndentedLine("{");
                    currentIndent++;
                    break;
                case ']':
                    currentIndent--;
                    appendIndentedLine("}");
                    break;
            }
        }

        sb.Append("    return 0;\n}");
        return sb.ToString();
    }

    public static string? RunCommand(string command)
    {
        var psi = new ProcessStartInfo();
#if WINDOWS
        psi.FileName = @"C:\Windows\System32\cmd.exe";
        psi.Arguments = command;
#else
        psi.FileName = "/bin/bash";
        psi.Arguments = $"-c \"{command}\"";
#endif
        psi.RedirectStandardOutput = true;
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;

        using var process = Process.Start(psi);
        if (process is null)
        {
            return null;
        }

        process.WaitForExit();

        var output = process.StandardOutput.ReadToEnd();

        return output;
    }

    static public void Interpret(string input)
    {
        var brackets = GetBraces(input);
        int indexCurrent = 0;
        var result = new List<byte>();
        for (int i = 0; i < 30_000; i++)
        {
            result.Add(0);
        }

        int indexResult = 0;
        while (indexCurrent < input.Length)
        {
            switch (input[indexCurrent])
            {
                case '>':
                    indexResult++;
                    if (indexResult == result.Count)
                    {
                        result.Add(0);
                    }
                    break;
                case '<':
                    indexResult--;
                    break;
                case '+':
                    result[indexResult] += 1;
                    break;
                case '-':
                    result[indexResult] -= 1;
                    break;
                case '.':
                    System.Console.Write((char)result[indexResult]);
                    break;
                case ',':
                    result[indexResult] = (byte)Console.Read();
                    break;
                case '[':
                    if (result[indexResult] == 0)
                    {
                        indexCurrent = brackets[indexCurrent];
                    }
                    break;
                case ']':
                    if (result[indexResult] != 0)
                    {
                        indexCurrent = brackets[indexCurrent];
                    }
                    break;
            }
            indexCurrent++;
        }
    }

    static Dictionary<int, int> GetBraces(string s)
    {
        var bracket = new Dictionary<int, int>();
        var stackBrackets = new Stack<int>();
        int index = 0;

        while (index < s.Length)
        {
            if (s[index] == '[')
            {
                stackBrackets.Push(index);
            }
            if (s[index] == ']')
            {
                int openingBracket = stackBrackets.Pop();
                bracket.Add(openingBracket, index);
                bracket.Add(index, openingBracket);
            }
            index++;
        }

        return bracket;
    }
}
