namespace Dungen;

public static class Config {
    public static float Tolerance {
        get => 0.0001f;
    }

    public static int TargetSolutionCount {
        get;
        set;
    } = 10;

    public static int MaxResets {
        get => 5;
    }

    public static int MaxTrialsPerCycle {
        get => 500;
    }

    public static int MaxCycles {
        get => 50;
    }   

    public static int MaxPartialLayouts {
        get => 15;
    }   

    public static float DoorWidth {
        get;
        set; 
    } = 1f;

    public static float DoorToCornerMinGap {
        get;
        set; 
    } = 1f;

    public static float AcceptProbabilityStart {
        get => 0.2f;
    } 

    public static float AcceptProbabilityEnd {
        get => 0.01f;
    } 

    public static float SignificantDistanceThreshold {
        get => 0.01f;
    } 
}