using System.Windows.Input;

namespace Daedalus.Core.Commands;

public interface ICommandHander<TCmd> where TCmd: ICommand {
    public void Execute(TCmd command);
}