namespace RealynxBot.Services.Interfaces {
    internal interface ILmCorrectGrammar {
        Task<string> CorrectGrammar(string prompt);
    }
}