using System.Windows.Input;

namespace Daedalus.Core.Commands;

public interface ICommandHander<TCmd, TRes> where TCmd: ICommand {
    public TRes Execute(TCmd command);
}