namespace Unidirect.Core.Logic
{
    public sealed class ResponseCommand
    {
        public readonly int Id;
        public readonly string Name;

        private ResponseCommand(int id, string name)
        {
            Id = id;
            Name = name;
        }

        private static int _uniqueId;
        
        public static ResponseCommand Get(string name)
        {
            return new ResponseCommand(_uniqueId++, name);
        }

        public static ResponseCommand Get(int id, string name)
        {
            return new ResponseCommand(id, name);
        }

        public static readonly ResponseCommand None = new(int.MinValue, "None");
    }
}