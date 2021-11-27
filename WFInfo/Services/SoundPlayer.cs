using System.IO;
using System.Reflection;

namespace WFInfo
{
    internal interface ISoundPlayer
    {
        void Play();
    }

    public class SoundPlayer : ISoundPlayer
    {
        private readonly System.Media.SoundPlayer _player;
        public SoundPlayer()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var audioStream = assembly.GetManifestResourceStream("WFInfo.Resources.achievment_03.wav");
            _player = new System.Media.SoundPlayer(audioStream);
        }

        public void Play()
        {
            _player.Play();
        }
    }
}