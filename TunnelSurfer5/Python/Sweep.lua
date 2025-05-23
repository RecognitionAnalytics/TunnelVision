
reset()
errorqueue.clear()
loadandrunscript myscript

-- The local functions are scoped at the project level, so that we can keep
-- track of the reading buffers throughout the test.
local systemNodes = {}
local systemSmus = {}
local systemSmuReadingBufferIndexes = {}
local appendMode = 1

--==========================================================================
-- This function tests for overruns in any of the system smus' trigger models.
-- It returns an error to the application that will be displayed in a
-- dialog box if an overrun occurs.
--==========================================================================
local _Overruncheck = function()
    --==========================================================================
    -- This function test the results of the value in the smu's trigger overrun
    -- status register. -- It prints an error if an overrun occurs.
    --
    -- parameter   overrun     A bit pattern that contains the smu trigger overrun
    --                         status register result
    --==========================================================================
    local TestOverrunBits = function(overrun)
        if(bit.test(overrun, 2)) then
            print("[{error}]Script stopped due to arm overrun." )
        elseif(bit.test(overrun, 3)) then
            print("[{error}]Script stopped due to source overrun." )
        elseif(bit.test(overrun, 4)) then
            print("[{error}]Script stopped due to measure overrun." )
        elseif(bit.test(overrun, 5)) then
        print("[{error}]Script stopped due to end pulse overrun." )
    end
end

for i, selNode in ipairs(systemNodes) do
    for j, selSmu in ipairs(systemSmus[i]) do
        if(j == 1) then
            local overrun = selNode.status.operation.instrument.smua.trigger_overrun.event
            TestOverrunBits(overrun)
        elseif(j == 2) then
            local overrun = selNode.status.operation.instrument.smub.trigger_overrun.event
            TestOverrunBits(overrun)
        end
    end
end
end

local gComplianceDetected = false
--==========================================================================
-- Checks smus for voltage or current compliance.
-- If a compliance event occurs, the message dialog box is presented to the
-- user.
--==========================================================================
local _ComplianceCheck = function()
    -- Report a compliance error only once.
    if(gComplianceDetected == false) then
        for i, selNode in ipairs(systemNodes) do
            for j, selSmu in ipairs(systemSmus[i]) do
                local compDetect = selNode.status.measurement.instrument.event
                
                if(compDetect > 0) then
                    local whichSmu = [[]]
                    if(compDetect == 2) then
                        whichSmu = [[smua]]
                    elseif (compDetect == 4) then
                        whichSmu = [[smub]]
                    elseif (compDetect == 6) then
                        whichSmu = [[smua and smub]]
                    end
                    
                    -- Set gComplianceDetected so we don't report the error again.
                    gComplianceDetected = true
                    print("[{info}]Compliance detected on ".. whichSmu .. [[.]] )
                    return
                end
            end
        end
    end
end

--==========================================================================
-- This function waits up to a specified period for the sweeps to complete.
-- If the delay parameter is -1, then the timeout is infinite.
-- It returns true if all sweeping actions are complete. Otherwise, the return
-- value is false.
--
-- parameter  interval     Maximum time to wait (in seconds).
--==========================================================================
local _WaitForComplete = function(interval)
    local pollInterval = 0.1
    
    --==========================================================================
    -- Checks the system smus for any that are still sweeping.
    --==========================================================================
    local IsSweepingComplete = function()
        for i, selNode in ipairs(systemNodes) do
            for j, selSmu in ipairs(systemSmus[i]) do
                if(j == 1) then
                    local statcond = selNode.status.operation.instrument.smua.condition
                    if(bit.test(statcond,4)) then
                        return false
                    end
                elseif(j == 2) then
                    local statcond = selNode.status.operation.instrument.smub.condition
                    if(bit.test(statcond,4)) then
                        return false
                    end
                end
            end
        end
        
        return true
    end
    
    local notimeout = false
    if(interval < 0) then
        notimeout = true
        interval = 1
    end
    
    while(interval > 0) do
        -- Check for overruns
        _Overruncheck(overrun)
        _ComplianceCheck()
        
        if(IsSweepingComplete() == true) then
            return true
        end
        
        delay(pollInterval)
        
        if(notimeout == false) then
            interval = interval - pollInterval
        end
    end
    
    if(IsSweepingComplete()) then
        return true
    end
    
    return false
end
----------------------------------------------------------------------------
-- START OF INITIALIZE SEGMENT ... do not modify this section
----------------------------------------------------------------------------
--==========================================================================
-- This function prepares the test for execution. 
-- It first verifies that current setup matches project's setup. 
-- Then, it initalizes members used to keep track of reading buffer storage.
--==========================================================================
function _Initialize()
    local maxNodes = 64   -- Maximum possible nodes in TSP link system.
    
    local projectSetup = {{localnode,[[2636A]],[[2.1.5]]}} -- Instrument configuration for the project.
    local currentSetup = {} -- Current instrument configuration.
    
    local errorTag = "[{error}]"
    local errorMessage = {[[Instrument in project configuration not found.]], [[Instrument configuration at does not match.]]}
    local errorNo = 0
    
    --==========================================================================
    -- Configures the status model to detect voltage or current compliance.
    --==========================================================================
    local ConfigStatusModel = function()
        for i, selNode in ipairs(systemNodes) do
            for j, selSmu in ipairs(systemSmus[i]) do
                -- The selSmu.source.compliance call will force an update
                -- the condition register of the measurement.instrument.smuX to
                -- update
                local dum = selSmu.source.compliance
                if(j == 1) then
                    selNode.status.measurement.instrument.smua.enable = 3
                    selNode.status.measurement.instrument.smua.ptr = 3
                    selNode.status.measurement.instrument.enable = 6
                    selNode.status.measurement.instrument.ptr = 6
                elseif(j == 2) then
                    selNode.status.measurement.instrument.smub.enable = 3
                    selNode.status.measurement.instrument.smub.ptr = 3
                end
            end
        end
    end
    
    --==========================================================================
    -- Determines the current system configuration.
    --==========================================================================
    local GetSetup = function ()
        local masterNode = tsplink.node
        
        systemSmus = {}
        systemNodes = {}
        systemNodes[1] = node[masterNode]
        
        currentSetup[1] = {}
        currentSetup[1][1] = node[masterNode]
        currentSetup[1][2] = node[masterNode].model
        currentSetup[1][3] = node[masterNode].revision
        
        systemSmus[1] = {}
        systemSmus[1][1] = node[masterNode].smua
        
        if(node[masterNode].smub ~= nil) then
            systemSmus[1][2] = node[masterNode].smub
        end
        
        local j = 2
        for i = 1, maxNodes do
            if((tsplink.node ~= i) and (node[i] ~= nil)) then
                systemNodes[j] = node[i]
                currentSetup[j] = {}
                currentSetup[j][1] = node[i]
                currentSetup[j][2] = node[i].model
                currentSetup[j][3] = node[i].revision
                
                systemSmus[j] = {}
                systemSmus[j][1] = node[i].smua
                
                if(node[i].smub ~= nil) then
                    systemSmus[j][2] = node[i].smub
                end
                j = j +1
            end
        end
    end
    
    GetSetup()
    
    -- Check the project setup versus the configuration setup.
    -- If there is a difference, report an error.
    local numpInstruments = table.getn(projectSetup)
    local numcInstruments = table.getn(currentSetup)
    
    for i=1, numpInstruments do
        local pfields = table.getn(projectSetup[i])
        
        errorNo = 1
        for j=1,numcInstruments do
            -- Compare nodes.
            if(projectSetup[i][1] ==  currentSetup[j][1]) then
                errorNo = 0
                
                for k=2, pfields do
                    if(projectSetup[i][k] ~= currentSetup[j][k]) then
                        errorNo = 2
                    end
                end
                break
            end
        end
        
        if (errorNo > 0) then
            break
        end
    end
    
    if (errorNo > 0) then
        error(errorMessage[errorNo])
        print(errorTag .. errorMessage[errorNo])
    end
    
    --==========================================================================
    -- Initializes all system readings buffers by clearing and setting them to
    -- append mode. An array, sysSmuReadingBufferIndexes, is used to keep track of the
    -- data stored to each of the reading buffers.
    --==========================================================================
    local InitalizeReadingBuffers = function()
        systemSmuReadingBufferIndexes = {}
        for i, selNode in ipairs(systemSmus) do
            systemSmuReadingBufferIndexes[i] = {}
            
            for j, selSmu in ipairs(selNode) do
                -- Create an array for the smu.
                systemSmuReadingBufferIndexes[i][j] = {}
                
                local snvBuffers =  {selSmu.nvbuffer1, selSmu.nvbuffer2}
                for k, selBuffer in ipairs(snvBuffers) do
                    -- Create an array for nvbuffers 1 and 2.
                    systemSmuReadingBufferIndexes[i][j][k] = {}
                    
                    local sysSmuReadingBufferIndexes = systemSmuReadingBufferIndexes[i][j][k]
                    sysSmuReadingBufferIndexes["start"] = 0
                    sysSmuReadingBufferIndexes["stop"] = 0
                    
                    selBuffer.clear()
                    selBuffer.appendmode = 1
                    selBuffer.collecttimestamps = 1
                    selBuffer.collectsourcevalues = 1
                end
                
            end
        end
    end
    
    InitalizeReadingBuffers()
    ConfigStatusModel()
end
----------------------------------------------------------------------------
-- END OF INITIALIZE SEGMENT ... do not modify code after this point
----------------------------------------------------------------------------

----------------------------------------------------------------------------
-- START OF SWEEP SEGMENT ... do not modify this section
----------------------------------------------------------------------------
--==========================================================================
-- Configures a sweeping test.
-- (This represents a Sweep segment of the script.  Click on the Sweep tab to customize it.)
--==========================================================================
function _Sweep()
    local errorTag = "[{error}]"
    local biasIndex = 1
    local stepIndex = 2
    local sweepIndex = 3
    local numLevels = 3
    local nvBuffersUsed = {1,1,1}
    -- local alias for SMUs
    local sweep1 = localnode.smub -- is a 2636A
    -- This function configures any number of sweeping smus with no stepping smus.
    -- It supports sweeps with autorange off and pulse mode.
    local testSmus = {
        {{}}, -- Bias
        {{}}, -- Step
        {{ localnode.smub }}, -- Sweep
    }
    
    local testNodes = {
        {}, -- Bias
        {}, -- Step
        {localnode} -- Sweep
    }
    
    local sweepPulseModes ={{ false }}
    
    local timePoint = tpXXXE-3
    local pulseWidth = pwXXXE-3
    local measurePointTime = 1.72666666667E-3
    
    local measureDelay = 0  -- This value may be used with a timer in place of measure and source delays.
    
    local numberOfSteps =  1
    local numberOfSweeps = nosXXX
    
    local nplc = 0.1
    local lineFrequency = 60
    
    local numNodes =table.getn(testNodes[sweepIndex])
    local numMasterSmus = table.getn(testSmus[sweepIndex][1])
    local masterNode = testNodes[sweepIndex][1]
    local masterSweepSmu = testSmus[sweepIndex][1][1]
    
    --==========================================================================
    -- This function checks for errors or conflicts in the test settings (timing, ranges, etc.)
    --==========================================================================
    local CheckSettings = function()
        measureDelay = timePoint - measurePointTime
        
        local errorMsg = nil
        
        -- Valid timing paramaters
        -- None of these errors should appear to the user.
        if(masterSweepSmu == nil) then
            errorMsg =  [[Must have at least one sweeping smu.]]
        elseif(timePoint <= 0) then
            errorMsg = [[timePoint <= 0]]
        elseif(pulseWidth <= 0) then
            errorMsg = [[pulseWidth <= 0]]
        elseif(pulseWidth > timePoint) then
            errorMsg = [[pulseWidth > timePoint]]
        elseif(measureDelay < 0) then
            errorMsg = [[timePoint too small, <= measurePointTime]]
        end
        
        if(errorMsg ~= nil) then
            print(errorTag.. errorMsg)
            error(errorMsg)
        end
        
        -- TODO: check settings
    end
    
    --==========================================================================
    -- This function configures the trigger line interaction between the smus.
    --==========================================================================
    local ConfigureTriggerLines = function()
        --==========================================================================
        -- This function configures the sweeping smus.
        --==========================================================================
        local ConfigureSweep = function()
            --==========================================================================
            -- Configures the timers uses in the test.
            --==========================================================================
            local ConfigureSweepTimers = function(selNode, stimulusEventId)
                local t1Timer = selNode.trigger.timer[1]
                local t2Timer = selNode.trigger.timer[2]
                
                t1Timer.delay = timePoint
                t1Timer.passthrough = true
                t1Timer.count = numberOfSweeps -1
                t1Timer.stimulus = stimulusEventId
                
                t2Timer.stimulus = t1Timer.EVENT_ID
                t2Timer.passthrough = false
                t2Timer.delay = pulseWidth
            end
            
            --==========================================================================
            -- Configures a paricular smu for sweeping.
            --
            -- parameter pulseMode                  true for Pulse mode,  false otherwise.
            -- parameter selNode                    Node to which the smu belongs.
            -- parameter selSmu                     Smu to configure.
            -- parameter armStimulusEventId         Arm stimulus event.
            --==========================================================================
            local ConfigureSweepSmu = function(pulseMode, selNode, selSmu, armStimulusEventId)
                local t1Timer = selNode.trigger.timer[1]
                local t2Timer = selNode.trigger.timer[2]
                
                selSmu.trigger.arm.stimulus = armStimulusEventId
                selSmu.trigger.source.stimulus = t1Timer.EVENT_ID
                
                if((pulseMode == true) and (pulseWidth < timePoint)) then
                    selSmu.trigger.measure.stimulus = 0
                    selSmu.trigger.endpulse.stimulus = t2Timer.EVENT_ID
                    selSmu.trigger.endpulse.action = selSmu.SOURCE_IDLE
                else
                    selSmu.trigger.measure.stimulus = 0
                    selSmu.trigger.endpulse.stimulus = 0
                    selSmu.trigger.endpulse.action = selSmu.SOURCE_HOLD
                end
                
                selSmu.trigger.arm.count = numberOfSteps
                selSmu.trigger.count = numberOfSweeps
                
                selSmu.trigger.measure.action = selSmu.ENABLE
                selSmu.trigger.source.action = selSmu.ENABLE
            end
            
            -- Configures the smus on the master node. The master node contains the
            -- smu that kicks off the sweeps.
            ConfigureSweepTimers(masterNode, masterSweepSmu.trigger.ARMED_EVENT_ID)
            
            for i=1, numMasterSmus do
                local locSmu = testSmus[sweepIndex][1][i]
                
                if (i==1) then
                    ConfigureSweepSmu(sweepPulseModes[1][i], masterNode, locSmu, masterNode.trigger.EVENT_ID)
                else
                    ConfigureSweepSmu(sweepPulseModes[1][i], masterNode, locSmu, masterSweepSmu.trigger.ARMED_EVENT_ID)
                end
            end
            
            if( numNodes > 1) then
                local masterTspLine1  = masterNode.tsplink.trigger[1]
                masterTspLine1.mode = tsplink.TRIG_FALLING
                masterTspLine1.stimulus = masterSweepSmu.trigger.ARMED_EVENT_ID
                
                for i=2, numNodes do
                    local selNode = testNodes[sweepIndex][i]
                    
                    local tspl1 = selNode.tsplink.trigger[1]
                    tspl1.mode = tsplink.TRIG_FALLING
                    tspl1.pulsewidth = 1e-7
                    tspl1.stimulus = 0
                    
                    ConfigureSweepTimers(selNode, tspl1.EVENT_ID)
                    
                    local sweepSmus = testSmus[sweepIndex]
                    local numSmus = table.getn(sweepSmus[i])
                    
                    for j= 1, numSmus do
                        local extSmu  = sweepSmus[i][j]
                        ConfigureSweepSmu(sweepPulseModes[i][j], selNode, extSmu, tspl1.EVENT_ID)
                    end
                end
            end
        end
        
        -- Configure step and sweep trigger lines.
        ConfigureSweep()
    end
    
    --==========================================================================
    -- This function kicks off the sweep.
    --==========================================================================
    local StartSweep = function()
        masterSweepSmu.trigger.arm.set()
    end
    --==========================================================================
    -- Resets all components that may be used in the test.
    --==========================================================================
    local Reset = function()
        for i, selTest in ipairs(testSmus) do
            for j, selNode in ipairs(testNodes[i]) do
                if(selNode ~= nil) then
                    -- Reset blenders
                    local k = 1
                    while(selNode.tsplink.trigger[k] ~= nil) do
                        selNode.tsplink.trigger[k].reset()
                        k= k+1
                    end
                    
                    k=1
                    while(selNode.trigger.blender[k] ~= nil) do
                        selNode.trigger.blender[k].reset()
                        k= k+1
                    end
                    
                    -- Reset Timers
                    k =1
                    while(selNode.trigger.timer[k] ~= nil) do
                        selNode.trigger.timer[k].reset()
                        k= k+1
                    end
                end
            end
            
            -- Reset Smus
            for j, selNode in ipairs(selTest) do
                for k, selSmu in ipairs(selNode) do
                    if(selSmu ~= nil) then
                        -- Resets all smus in the test.
                        selSmu.reset()
                    end
                end
            end
        end
    end
    
    --==========================================================================
    -- Turns all smus in the test on.
    --==========================================================================
    local TurnSmusOn = function()
        for i, selTest in ipairs(testSmus) do
            for j, selNode in ipairs(selTest) do
                for k, selSmu in ipairs(selNode) do
                    if(selSmu ~= nil) then
                        if(selSmu.source.output ~= 1) then
                            selSmu.source.output = 1
                        end
                    end
                end
            end
        end
    end
    
    --==========================================================================
    -- This function initiates the steping and sweeping smus.
    --==========================================================================
    local InitiateSmus = function()
        for i=2,numLevels do
            for j, selNode in ipairs(testSmus[i]) do
                for k, selSmu in ipairs(selNode) do
                    if(selSmu ~= nil) then
                        selSmu.trigger.initiate()
                    end
                end
            end
        end
    end
    
    --==========================================================================
    -- This function determines the number of points that will be in the buffer at the end of the test.
    --==========================================================================
    local StoreReadingBufferIndexes = function()
        --==========================================================================
        -- Determines reading buffer starting and stopping indexes.
        -- parameter    testLevel    Index referring to bias, step, or sweep.
        -- parameter    i            Node index.
        -- parameter    selSmuIndex  Selected smu index.
        --==========================================================================
        local SetupLastPoints = function(testLevel, i, selSmuIndex)
            local stoppingPoint1 = 0
            local stoppingPoint2 = 0
            local incVal = 0
            local selSmuRbIndex1 = systemSmuReadingBufferIndexes[i][selSmuIndex][1]
            local selSmuRbIndex2 = systemSmuReadingBufferIndexes[i][selSmuIndex][2]
            
            selSmuRbIndex1["start"] = 1
            selSmuRbIndex2["start"] = 1
            
            -- appendMode is a local variable common to the script.
            if(appendMode == 1) then
                stoppingPoint1 = selSmuRbIndex1["stop"]
                stoppingPoint2 = selSmuRbIndex2["stop"]
            end
            -- Bias setup
            if(testLevel == biasIndex) then
                incVal = 1
                -- Step setup
            elseif (testLevel == stepIndex) then
                incVal = (1 * 1)
                -- Sweep setup
            elseif (testLevel == sweepIndex) then
                incVal = (1 * nosXXX * 1)
            end
            
            selSmuRbIndex1["stop"] = stoppingPoint1 + incVal
            if(nvBuffersUsed[testLevel] == 2) then
                selSmuRbIndex2["stop"] = stoppingPoint2 + incVal
            end
        end
        
        for i, selTest in ipairs(testSmus) do
            for j, selNode in ipairs(selTest) do
                for k, selSmu in ipairs(selNode) do
                    for l, selSysNode in ipairs(systemSmus) do
                        if(selSysNode[1] == selSmu) then
                            SetupLastPoints(i, l, 1)
                        end
                        
                        if(selSysNode[2] ~= nil) then
                            if(selSysNode[2] == selSmu) then
                                SetupLastPoints(i, l, 2)
                            end
                        end
                    end
                end
            end
        end
    end
    ---==========================================================================
    -- The following makes sure we capture a measurement from the bias channels
    --==========================================================================
    local MeasureBiasChannels = function()
    end
    --==========================================================================
    -- This function Configures the smu options such as nplc, function, range, etc.
    --==========================================================================
    local ConfigureSmus = function()
        
        -- Set up bias SMUs
        -- set up Sweep (inner sweep) for sweep1
        sweep1.source.delay = 0
        sweep1.sense = sweep1.SENSE_LOCAL
        sweep1.measure.nplc = nplc
        sweep1.measure.autozero = sweep1.AUTOZERO_ONCE
        sweep1.measure.count = 1
        sweep1.measure.filter.enable = sweep1.FILTER_OFF
        sweep1.measure.filter.type = sweep1.FILTER_MOVING_AVG
        sweep1.measure.filter.count = 1
        sweep1.measure.delay = 0
        sweep1.measure.delayfactor = 1
        sweep1.measure.analogfilter = 0
        sweep1.source.func = sweep1.OUTPUT_DCVOLTS
        if sweep1.source.highc == sweep1.ENABLE then sweep1.source.highc = sweep1.DISABLE end
        sweep1.source.limiti = 1E-3
        sweep1.trigger.source.limiti = sweep1.LIMIT_AUTO
        sweep1.source.autorangev = 0
        sweep1.source.rangev = 2
        sweep1.trigger.source.listv({})
        sweep1.measure.autorangei = 0
        sweep1.measure.rangei = rangeiXXX
        sweep1.measure.autorangev = sweep1.source.autorangev
        if (sweep1.measure.autorangev == 0) then
            sweep1.measure.rangev = sweep1.source.rangev
        end
        sweep1.trigger.measure.iv(sweep1.nvbuffer1, sweep1.nvbuffer2)
        nvBuffersUsed[sweepIndex] = 2
    end
    -- Test starts here.
    
    -- Make sure previous test is done.
    
    _WaitForComplete(-1)
    
    CheckSettings()
    Reset()
    TurnSmusOn()
    ConfigureSmus()
    ConfigureTriggerLines()
    
    StoreReadingBufferIndexes()
    MeasureBiasChannels()
    
    InitiateSmus()
    StartSweep()
end
----------------------------------------------------------------------------
-- END OF SWEEP SEGMENT ... do not modify code after this point
----------------------------------------------------------------------------

----------------------------------------------------------------------------
-- START OF DATAREPORT SEGMENT ... do not modify this section
----------------------------------------------------------------------------
--==========================================================================
-- This script returns a series of reading buffers. 
-- (This represents a DataReport segment of the script.  Click on the Data tab to customize it.)
--==========================================================================
function _Data()
    local buffers = {localnode.smub.nvbuffer1,localnode.smub.nvbuffer2} -- Array of reading buffers to return
    local bufferNames = {[[localnode.smub.nvbuffer1]],[[localnode.smub.nvbuffer2]]} -- Array of reading buffer names
    local expectedCount = {} -- Array containing the expected number of return values.
    local waitInterval = 1  -- Interval between message transfers
    local maxToReturn = 50 -- Maximum reading buffer points between message transfers.
    
    -- The following are special tokens used internally by the data report function
    local DATA_REPORT = "(({{Data}}))"
    local SWEEPSTART = "{SWEEP-START}"
    local START = "{START}"
    local NAME = "{NAME}"
    local EXPECTED_COUNT = "{EXPECTED-COUNT}"
    local PTS_IN_BUFF = "{PTS-IN-BUFF}"
    local PTS_RETURNED =  "{PTS-RETURNED}"
    local BASE_TIME_STAMP = "{BASE-TIME-STAMP}"
    local READINGS = "{READINGS}"
    local TIMESTAMPS = "{TIMESTAMPS}"
    local SRCVALS = "{SRCVALS}"
    local END = "{END}"
    local COMPLETE = "{COMPLETE}"
    
    local errorTag = "[{error}]"
    local dataIndexes = {}
    local done = true
    local dataComplete = true
    
    --==========================================================================
    -- This function determines the ending points that will be in the buffer
    -- at the time data collection takes place.
    --==========================================================================
    local GetStopPoints = function()
        for i, selBuffer in ipairs(buffers) do
            for j, selSysNode in ipairs(systemSmus) do
                for k, selSmu in ipairs(selSysNode) do
                    if (selSmu.nvbuffer1 == selBuffer) then
                        expectedCount[i] = systemSmuReadingBufferIndexes[j][k][1]["stop"]
                    elseif (selSmu.nvbuffer2 == selBuffer) then
                        expectedCount[i] = systemSmuReadingBufferIndexes[j][k][2]["stop"]
                    end
                end
            end
        end
    end
    
    --==========================================================================
    -- Clears the reading buffers and resets the array that contains the
    -- reading buffer storage start/ stop points.
    --==========================================================================
    local ClearReturnedBuffers = function()
        for i, selBuffer in ipairs(buffers) do
            selBuffer.clear()
            
            local smuBufferIndex = nil
            for j, selSysNode in ipairs(systemSmus) do
                for k, selSmu in ipairs(selSysNode) do
                    if (selSmu.nvbuffer1 == selBuffer) then
                        smuBufferIndex = systemSmuReadingBufferIndexes[j][k][1]
                    elseif (selSmu.nvbuffer2 == selBuffer) then
                        smuBufferIndex = systemSmuReadingBufferIndexes[j][k][2]
                    end
                end
            end
            if(smuBufferIndex ~= nil) then
                smuBufferIndex["start"] = 0
                smuBufferIndex["stop"] = 0
            end
        end
    end
    
    GetStopPoints()
    
    -- Initialize dataIndex. This array is used to keep track of the points returned to
    -- the application.
    
    for i, v in ipairs(buffers) do
        dataIndexes[i] = 1
    end
    
    -- The following print statements turns on data report feature within the application.
    print(DATA_REPORT)
    print(SWEEPSTART)
    
    repeat
        done = true
        dataComplete = true
        for i, selBuffer in ipairs(buffers) do
            local numPointsStored = selBuffer.n
            
            -- Check for smu trigger model overruns.
            _Overruncheck()
            
            -- Check for smu compliance.
            _ComplianceCheck()
            
            dataComplete = dataComplete and (numPointsStored >= expectedCount[i])
            done = done and (dataIndexes[i] > expectedCount[i])
            
            if(dataIndexes[i] <= numPointsStored) then
                local points = numPointsStored - dataIndexes[i] + 1
                
                if(points > maxToReturn) then
                    points = maxToReturn
                end
                
                local retString = string.format("%s%s%s%d%s%d%s", NAME,  bufferNames[i], PTS_IN_BUFF, numPointsStored, PTS_RETURNED, points, START)
                
                if(dataIndexes[i] == 1) then
                    retString = retString .. string.format("%s%d%s%f", EXPECTED_COUNT, expectedCount[i], BASE_TIME_STAMP, selBuffer.basetimestamp)
                end
                
                local start = dataIndexes[i]
                local stop = start + points -1
                
                local readings =""
                
                for j=start, stop do
                    if(j == start) then
                        readings = readings .. string.format("%s%e", READINGS, selBuffer.readings[j])
                    else
                        readings = readings .. string.format(",%e", selBuffer.readings[j])
                    end
                end
                
                local timestamps = ""
                if (selBuffer.collecttimestamps == 1) then
                    for j=start, stop do
                        if(j == start) then
                            timestamps = timestamps .. string.format("%s%e", TIMESTAMPS, selBuffer.timestamps[j])
                        else
                            timestamps = timestamps .. string.format(",%e", selBuffer.timestamps[j])
                        end
                    end
                end
                
                local srcValues = ""
                if (selBuffer.collectsourcevalues == 1) then
                    for j=start, stop do
                        if(j == start) then
                            timestamps = timestamps .. string.format("%s%e", SRCVALS, selBuffer.sourcevalues[j])
                        else
                            timestamps = timestamps .. string.format(",%e", selBuffer.sourcevalues[j])
                        end
                    end
                end
                
                dataIndexes[i] = stop +1
                
                retString = retString .. readings .. timestamps .. srcValues .. END
                print(retString)
            end
        end
        
        if(dataComplete == false) then
            delay(waitInterval)
        end
    until (done == true)
    
    print(COMPLETE .. "\n(({{}}))")
    
    -- The application may not have selected all the buffers being used in the test, so
    -- we need to make sure the overlapped operations are complete before clearing the buffers.
    _WaitForComplete(-1)
    
    ClearReturnedBuffers()
end
----------------------------------------------------------------------------
-- END OF DATAREPORT SEGMENT ... do not modify code after this point
----------------------------------------------------------------------------

----------------------------------------------------------------------------
-- START OF FINALIZE SEGMENT ... do not modify this section
----------------------------------------------------------------------------
--==========================================================================
-- The function completes the script and places the instrument in a known state.
--==========================================================================
function _Finalize()
    _WaitForComplete(-1)
    
    -- Reset the system,  placing it into a known safe state.
    reset()
end
----------------------------------------------------------------------------
-- END OF FINALIZE SEGMENT ... do not modify code after this point
----------------------------------------------------------------------------

_Initialize()
_Sweep()
_Data()
_Finalize()
endscript