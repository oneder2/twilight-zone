public class BeforeSceneUnloadEvent
{
    public int StageId {get; private set;}
    public BeforeSceneUnloadEvent(){}
}

public class AfterSceneUnloadEvent
{
    public int StageId {get; private set;}
    public AfterSceneUnloadEvent(){}
}

public class AfterSceneLoadEvent
{
    public int StageId {get; private set;}
    public AfterSceneLoadEvent(){}
}
