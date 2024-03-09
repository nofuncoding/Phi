using Godot;
using System.IO;
using System.Linq;

namespace Phi;

public class AudioLoader
{
    public static AudioStreamMP3 LoadMP3(string path)
    {
        var sound = new AudioStreamMP3
        {
            Data = File.ReadAllBytes(path)
        };
        return sound;
    }

    public static AudioStreamWav LoadWAV(string path)
    {
        var raw_data = File.ReadAllBytes(path);
		
        // Parsing
        var bits_per_sample = 0;

        AudioStreamWav stream = new();

        foreach (var i in System.Linq.Enumerable.Range(0, 100))
        {
            var j = i + 3 + 1;
            var those4Bytes = System.Text.Encoding.ASCII.GetString(raw_data[i..j]);
        
            // GD.Print(those4bytes);

            if (those4Bytes == "RIFF")
                GD.Print("RIFF OK at bytes ", i, "-", i+3);
            
            if (those4Bytes == "WAVE")
                GD.Print("WAVE OK at bytes ", i, "-", i+3);

            if (those4Bytes == "fmt ")
            {
                GD.Print("fmt OK at bytes ", i, "-", i+3);

                // get format subchunk size, 4 bytes next to "fmt " are an int32
				var formatSubChunkSize = raw_data[i+4] + (raw_data[i+5] << 8) + (raw_data[i+6] << 16) + (raw_data[i+7] << 24);
				GD.Print("Format subchunk size: ", formatSubChunkSize);

                // using formatsubchunk index so it's easier to understand what's going on
				var fsc0 = i + 8; // fsc0 is byte 8 after start of "fmt "
            
                // get format code [Bytes 0-1]
				var format_code = raw_data[fsc0] + (raw_data[fsc0+1] << 8);
				string format_name;
				if (format_code == 0) format_name = "8_BITS";
				else if (format_code == 1) format_name = "16_BITS";
				else if (format_code == 2) format_name = "IMA_ADPCM";
				else
                {
					format_name = "UNKNOWN (trying to interpret as 16_BITS)";
					format_code = 1;
                }
				GD.Print("Format: ", format_code, " ", format_name);

                // assign format code to stream.
                stream.Format = (AudioStreamWav.FormatEnum)format_code;

                // get channel num [Bytes 2-3]
				var channel_num = raw_data[fsc0+2] + (raw_data[fsc0+3] << 8);
				GD.Print("Number of channels: ", channel_num);
				// set our AudioStreamSample to stereo if needed
				if (channel_num == 2) stream.Stereo = true;
				
				// get sample rate [Bytes 4-7]
				var sample_rate = raw_data[fsc0+4] + (raw_data[fsc0+5] << 8) + (raw_data[fsc0+6] << 16) + (raw_data[fsc0+7] << 24);
				GD.Print("Sample rate: ", sample_rate);
				// set our AudioStreamSample mixrate
				stream.MixRate = sample_rate;
				
				// get byte_rate [Bytes 8-11] because we can
				var byte_rate = raw_data[fsc0+8] + (raw_data[fsc0+9] << 8) + (raw_data[fsc0+10] << 16) + (raw_data[fsc0+11] << 24);
				GD.Print("Byte rate: " , byte_rate);
				
				// same with bits*sample*channel [Bytes 12-13]
				var bits_sample_channel = raw_data[fsc0+12] + (raw_data[fsc0+13] << 8);
				GD.Print("BitsPerSample * Channel / 8: ", bits_sample_channel);
				
				// aaaand bits per sample/bitrate [Bytes 14-15]
				bits_per_sample = raw_data[fsc0+14] + (raw_data[fsc0+15] << 8);
				GD.Print("Bits per sample: ", bits_per_sample);
            }

            if (those4Bytes == "data")
            {
                if (bits_per_sample == 0) throw new System.Exception("Bits per sample error");
				
				var audio_data_size = raw_data[i+4] + (raw_data[i+5] << 8) + (raw_data[i+6] << 16) + (raw_data[i+7] << 24);
				GD.Print("Audio data/stream size is ", audio_data_size, " bytes");

				var data_entry_point = i+8;
				GD.Print("Audio data starts at byte ", data_entry_point);

                // var data = raw_data.subarray(data_entry_point, data_entry_point+audio_data_size-1)
                var a = data_entry_point + audio_data_size;
                var data = raw_data[data_entry_point..a];

				//if (bits_per_sample in [24, 32])
				if (System.Linq.Enumerable.Range(24, 32).Contains(bits_per_sample))
					stream.Data = ConvertTo16Bit(data, bits_per_sample);
				else
					stream.Data = data;
            }
                
            // End of parsing
        }

        // get samples and set loop end
        var samplenum = stream.Data.Length / 4;
        stream.LoopEnd = samplenum;
        // stream.LoopMode = 1; // change to 0 or delete this line if you don't want loop, also check out modes 2 and 3 in the docs;
        return stream;
    }

    static byte[] ConvertTo16Bit(byte[] data, int from)
    {
        GD.Print($"converting to 16-bit from {from}");
        var time = Time.GetTicksMsec();
        // TODO
        /*// 24 bit .wav's are typically stored as integers
        // so we just grab the 2 most significant bytes and ignore the other
        if (from == 24)
        {
            var j = 0;
            foreach (var i in range(0, data.Length, 3))
            {
                data[j] = data[i+1]
                data[j+1] = data[i+2]
                j += 2
            data.resize(data.size() * 2 / 3)

            }
        }
        // 32 bit .wav's are typically stored as floating point numbers
        // so we need to grab all 4 bytes and interpret them as a float first
        if from == 32:
            var spb := StreamPeerBuffer.new()
            var single_float: float
            var value: int
            for i in range(0, data.size(), 4):
                spb.data_array = data.subarray(i, i+3)
                single_float = spb.get_float()
                value = single_float * 32768
                data[i/2] = value
                data[i/2+1] = value >> 8
            data.resize(data.size() / 2)*/
        GD.Print($"Took {(Time.GetTicksMsec() - time) / 1000.0} seconds for slow conversion");
        return data;
    }

    public static AudioStreamOggVorbis LoadOGG(string path)
    {
        return AudioStreamOggVorbis.LoadFromFile(path);
    }
}