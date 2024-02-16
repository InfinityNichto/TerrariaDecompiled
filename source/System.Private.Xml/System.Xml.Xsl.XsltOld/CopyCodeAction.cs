using System.Collections;

namespace System.Xml.Xsl.XsltOld;

internal sealed class CopyCodeAction : Action
{
	private readonly ArrayList _copyEvents;

	internal CopyCodeAction()
	{
		_copyEvents = new ArrayList();
	}

	internal void AddEvent(Event copyEvent)
	{
		_copyEvents.Add(copyEvent);
	}

	internal void AddEvents(ArrayList copyEvents)
	{
		_copyEvents.AddRange(copyEvents);
	}

	internal override void ReplaceNamespaceAlias(Compiler compiler)
	{
		int count = _copyEvents.Count;
		for (int i = 0; i < count; i++)
		{
			((Event)_copyEvents[i]).ReplaceNamespaceAlias(compiler);
		}
	}

	internal override void Execute(Processor processor, ActionFrame frame)
	{
		switch (frame.State)
		{
		default:
			return;
		case 0:
			frame.Counter = 0;
			frame.State = 2;
			break;
		case 2:
			break;
		}
		while (processor.CanContinue)
		{
			Event @event = (Event)_copyEvents[frame.Counter];
			if (@event.Output(processor, frame))
			{
				if (frame.IncrementCounter() >= _copyEvents.Count)
				{
					frame.Finished();
					break;
				}
				continue;
			}
			break;
		}
	}
}
