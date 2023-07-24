namespace ProjectCommons.DataTypes {
    public interface IMessageContent {
    }

    public class MessageContentText : IMessageContent {
        public MessageContentText(string textToSend) {
            Text = textToSend;
        }

        public string Text { get; set; }
    }
    
    
    public class MessageContentVoiceNote : IMessageContent {
        public string Path { get; set; }

        public MessageContentVoiceNote(string path) {
            Path = path;
        }
    }

    public class MessageContentPhoto : IMessageContent {
        public string Path { get; set; }

        public MessageContentPhoto(string photoPath) {
            Path = photoPath;
        }
    }

    public class MessageContentAudio : IMessageContent {
        public string Path { get; set; }

        public MessageContentAudio(string path) {
            Path = path;
        }
    }

    public class MessageContentVideo : IMessageContent {
        public string Path { get; set; }

        public MessageContentVideo(string path) {
            Path = path;
        }
    }
}