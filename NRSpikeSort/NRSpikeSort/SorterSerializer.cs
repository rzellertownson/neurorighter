using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace NRSpikeSort
{
    public class SorterSerializer
    {
        /// <summary>
        /// Serialization object for saving a spike sorter for resuse later.
        /// </summary>
        public SorterSerializer()
        {
        }
        
        /// <summary>
        /// Serializer for storing a spike sorter to disk.
        /// </summary>
        /// <param name="filename">Full path of spike sorter file.</param>
        /// <param name="sorterToSerialize"> The SpikeSorter object to be saved.</param>
        public void SerializeObject(string filename, SpikeSorter sorterToSerialize)
        {
            Stream stream = File.Open(filename, FileMode.Create);
            BinaryFormatter bFormatter = new BinaryFormatter();
            bFormatter.Serialize(stream, sorterToSerialize);
            stream.Close();
        }
        
        /// <summary>
        /// DeSerializer for loading a SpikeSorter object.
        /// </summary>
        /// <param name="filename">Full path of spike sorter file.</param>
        /// <returns></returns>
        public SpikeSorter DeSerializeObject(string filename)
        {
            SpikeSorter sorterToSerialize;
            Stream stream = File.Open(filename, FileMode.Open);
            BinaryFormatter bFormatter = new BinaryFormatter();
            sorterToSerialize = (SpikeSorter)bFormatter.Deserialize(stream);
            stream.Close();
            return sorterToSerialize;
        }
    }
}