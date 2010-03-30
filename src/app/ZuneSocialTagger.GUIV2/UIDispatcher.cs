using System.Windows.Threading;

namespace ZuneSocialTagger.GUIV2
{
    public static class UIDispatcher
    {
        private static Dispatcher _dispatcher;

        public static void SetDispatcher(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public static Dispatcher GetDispatcher()
        {
            return _dispatcher;
        }
    }
}