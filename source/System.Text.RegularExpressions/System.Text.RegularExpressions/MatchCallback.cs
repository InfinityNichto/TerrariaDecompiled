namespace System.Text.RegularExpressions;

internal delegate bool MatchCallback<TState>(ref TState state, Match match);
