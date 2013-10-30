﻿// OsmSharp - OpenStreetMap (OSM) SDK
// Copyright (C) 2013 Abelshausen Ben
// 
// This file is part of OsmSharp.
// 
// OsmSharp is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// OsmSharp is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with OsmSharp. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ProtoBuf.Meta;

namespace OsmSharp.Collections.Tags.Serializer
{
    /// <summary>
    /// Contains serialize/deserialize functionalities.
    /// </summary>
    public class TagIndexSerializer
    {
        /// <summary>
        /// Creates the type model.
        /// </summary>
        /// <returns></returns>
        private static RuntimeTypeModel CreateTypeModel()
        {
            RuntimeTypeModel typeModel = TypeModel.Create();
            typeModel.Add(typeof(List<string>), true);
            typeModel.Add(typeof(KeyValuePair<uint, uint>), true);
            typeModel.Add(typeof(List<KeyValuePair<uint, uint>>), true);
            typeModel.Add(typeof(List<KeyValuePair<uint, List<KeyValuePair<uint, uint>>>>), true);

            return typeModel;
        }

        /// <summary>
        /// Serializes all the tags in the given index. This serialization preserves the id's of each tag collection.
        /// </summary>
        /// <param name="stream">The target stream.</param>
        /// <param name="tagsIndex">The tags index to serialize.</param>
        public static void Serialize(Stream stream, ITagsIndex tagsIndex)
        {
            // build a string index.
            ObjectTable<string> stringTable = new ObjectTable<string>(false);

            // convert tag collections to simpler objects.
            List<KeyValuePair<uint, List<KeyValuePair<uint, uint>>>> tagIndex = new List<KeyValuePair<uint,List<KeyValuePair<uint,uint>>>>();
            for (uint tagId = 0; tagId < tagsIndex.Count; tagId++)
            {
                TagsCollection tagsCollection = tagsIndex.Get(tagId);
                if (tagsCollection != null)
                { // convert the tags collection to a list and add to the tag index.
                    List<KeyValuePair<uint, uint>> tagsList = new List<KeyValuePair<uint, uint>>();
                    foreach (Tag tag in tagsCollection)
                    {
                        uint keyId = stringTable.Add(tag.Key);
                        uint valueId = stringTable.Add(tag.Value);

                        tagsList.Add(new KeyValuePair<uint, uint>(
                            keyId, valueId));
                    }
                    tagIndex.Add(new KeyValuePair<uint, List<KeyValuePair<uint, uint>>>(tagId, tagsList));
                }
            }

            RuntimeTypeModel typeModel = TagIndexSerializer.CreateTypeModel();

            // move until after the index (index contains two int's, startoftagindex, endoffile).
            stream.Seek(8, SeekOrigin.Begin);

            // serialize string table.
            List<string> strings = new List<string>();
            for (uint id = 0; id < stringTable.Count; id++)
            {
                strings.Add(stringTable.Get(id));
            }
            stringTable.Clear();
            stringTable = null;
            typeModel.Serialize(stream, strings);
            long startOfTagsIndex = stream.Position;

            // serialize tagindex.
            typeModel.Serialize(stream, tagIndex);
            long endOfFile = stream.Position;

            // write index.
            stream.Seek(0, SeekOrigin.Begin);
            stream.Write(BitConverter.GetBytes((int)startOfTagsIndex), 0, 4); // write start position of tagindex.
            stream.Write(BitConverter.GetBytes((int)endOfFile), 0, 4); // write size of complete file.
            
            // clear everything.
            tagIndex.Clear();
        }

        /// <summary>
        /// Deserializes a tags index from the given stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static ITagsIndex Deserialize(Stream stream)
        {
            throw new NotImplementedException();
        }
    }
}
