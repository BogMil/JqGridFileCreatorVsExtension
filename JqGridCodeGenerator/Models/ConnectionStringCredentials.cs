using System.ComponentModel;

namespace JqGridCodeGenerator.Models
{
    class ConnectionStringCredentials
    {
        public ConnectionStringCredentials()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private string _server = "Server";
        private string _username = "Manualy add columns and their definition";
        private string _password = "TestTableId";
        private bool _isWindowsAuthentication = false;

        public string Server
        {
            get { return _server; }
            set
            {
                if (_server != value)
                    _server = value;
            }
        }

        public string Username
        {
            get { return _username; }
            set
            {
                if (_username != value)
                    _username = value;
            }
        }

        public string Password
        {
            get { return _password; }
            set
            {
                if (_password != value)
                {
                    _password = value;

                    onPropertyChanged("TableId");
                }
            }
        }

        public bool IsWindowsAuthentication
        {
            get { return _isWindowsAuthentication; }
            set
            {
                if (_isWindowsAuthentication != value)
                {
                    _isWindowsAuthentication = value;
                    onPropertyChanged("PagerId");
                }
            }
        }

        private void onPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}

