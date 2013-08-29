using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lewt.Shared
{
    public class Log
    {
        private Queue<String> myMessages;

        public readonly int LineCapacity;
        public bool PrintToConsole = false;

        public Log( int capacity = 256 )
        {
            LineCapacity = capacity;
            myMessages = new Queue<string>( capacity );
        }

        public void Add( String message )
        {
            if ( myMessages.Count == LineCapacity )
                myMessages.Dequeue();

            myMessages.Enqueue( message );

            if ( PrintToConsole )
                Console.WriteLine( message );
        }

        public bool ContainsUnread
        {
            get
            {
                return myMessages.Count > 0;
            }
        }

        public String ReadLine()
        {
            return myMessages.Dequeue();
        }
    }
}
