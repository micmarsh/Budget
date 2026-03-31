// See https://aka.ms/new-console-template for more information

using Budget.CommandLine;
using CommandLine.Immutable;

Cmd.New("budget", "A suite of tools for managing a household budget")
    .AddSub(Migration.Command)
    .ToRoot()
    .Parse(args)
    .Invoke();

