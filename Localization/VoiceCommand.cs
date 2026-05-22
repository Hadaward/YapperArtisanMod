using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Artisan.Localization
{
    public sealed class ArtisanVoiceCommand
    {
        public string Key { get; private set; }
        public string Command { get; private set; }
        public bool OverwriteExisting { get; private set; }

        public ArtisanVoiceCommand(string key, string command, bool overwriteExisting = false)
        {
            Key = key;
            Command = command;
            OverwriteExisting = overwriteExisting;
        }
    }
}
