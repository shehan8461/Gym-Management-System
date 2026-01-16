using System.Windows;
using GymManagementSystem.Data;
using System.Linq;

namespace GymManagementSystem.Views.Dialogs
{
    public partial class MemberActionsDialog : Window
    {
        private int _memberId;
        public bool ActionTaken { get; private set; } = false;

        public MemberActionsDialog(int memberId)
        {
            InitializeComponent();
            _memberId = memberId;
            LoadMemberInfo();
        }

        private void LoadMemberInfo()
        {
            using (var context = new GymDbContext())
            {
                var member = context.Members.FirstOrDefault(m => m.MemberId == _memberId);
                if (member != null)
                {
                    txtMemberName.Text = $"Actions for {member.FullName}";
                    txtMemberInfo.Text = $"Member ID: {member.MemberId}";
                }
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddEditMemberDialog(_memberId);
            if (dialog.ShowDialog() == true)
            {
                ActionTaken = true;
            }
            this.Close();
        }

        private void btnPackage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AssignPackageDialog(_memberId);
            if (dialog.ShowDialog() == true)
            {
                ActionTaken = true;
            }
            this.Close();
        }

        private void btnPayment_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddPaymentDialog(_memberId);
            if (dialog.ShowDialog() == true)
            {
                ActionTaken = true;
            }
            this.Close();
        }

        private void btnHistory_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new MemberHistoryDialog(_memberId);
            dialog.ShowDialog();
            this.Close();
        }

        private void btnBiometric_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new EnrollFingerprintDialog(_memberId);
            dialog.ShowDialog();
            this.Close();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            ActionTaken = true;
            this.DialogResult = true;
            this.Close();
        }
    }
}
