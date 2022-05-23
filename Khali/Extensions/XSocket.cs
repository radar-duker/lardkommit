using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

namespace Khali.Extensions {

    public static class XSocket {

        public static T Read<T>(this Socket s) {
            var formatter = new BinaryFormatter();
            var stream = new NetworkStream(s);
            return (T) formatter.Deserialize(stream);
        }

        public static void Write<T>(this Socket s, T o) {
            var formatter = new BinaryFormatter();
            var stream = new NetworkStream(s);
            formatter.Serialize(stream, o);
        }

    }

}