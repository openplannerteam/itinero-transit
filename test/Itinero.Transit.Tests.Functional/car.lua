-- car globals
name = "car"
vehicle_types = { "vehicle", "motor_vehicle", "motorcar" }
constraints = { "maxweight", "maxwidth" }
normalize = false

minspeed = 30
maxspeed = 200

-- default speed profiles
speed_profile = {
	["motorway"] = 120,
	["motorway_link"] = 120,
	["trunk"] = 90,
	["trunk_link"] = 90,
	["primary"] = 90,
	["primary_link"] = 90,
	["secondary"] = 60,
	["secondary_link"] = 60,
	["tertiary"] = 50,
	["tertiary_link"] = 50,
	["unclassified"] = 30,
	["residential"] = 30,
	["service"] = 20,
	["services"] = 20,
	["road"] = 20,
	["track"] = 20,
	["living_street"] = 5,
	["ferry"] = 5,
	["movable"] = 5,
	["shuttle_train"] = 10,
  	["default"] = 10
}

-- default access values
access_factor_no = 0 -- totally no access.
access_factor_local = 0.01 -- only access when absolutely necessary.
access_factor_avoid = 0.1 -- can access but try to avoid.
access_factor_yes = 1 -- normal access.

access_values = {
    ["designated"] = access_factor_yes,
    ["public"] = access_factor_yes,
    ["yes"] = access_factor_yes,
    ["permissive"] = access_factor_avoid,
    ["destination"] = access_factor_avoid,
    ["delivery"] = access_factor_avoid,
    ["service"] = access_factor_avoid,
    ["customers"] = access_factor_local,
    ["private"] = access_factor_local,
    ["no"] = access_factor_no,
    ["use_sidepath"] = access_factor_no,
    ["gate"] = access_factor_no,
    ["bollard"] = access_factor_no
}

-- whitelists for profile and meta
profile_whitelist = {
    "highway",
    "oneway",
    "motorcar",
    "motor_vehicle",
    "vehicle",
    "access",
    "maxspeed",
    "maxweight",
    "maxwidth",
    "junction",
    "route",
    "anyways:construction",
    "anyways:access",
    "anyways:vehicle", 
    "anyways:motor_vehicle", 
    "anyways:motorcar"
}
meta_whitelist = {
    "name",
    "anyways:new",
    "anyways:detour"
}

-- profile definitions linking a function to a profile
profiles = {
	{ -- do not use, either incorrect time estimates or incorrect access restrictions.
        name = "",
        function_name = "factor_and_speed",
        metric = "time"
    },
	{ -- do not use, either incorrect time estimates or incorrect access restrictions.
        name = "shortest",
        function_name = "factor_and_speed",
        metric = "distance",
    },
    { -- this is the OPA profile, use this one for OPA routing.
        name = "opa",
        function_name = "factor_and_speed",
        metric = "custom"
    },
    { -- this is the default profile, use this one for default routing.
        name = "default",
        function_name = "factor_and_speed",
        metric = "custom"
    },
    { -- this is the classifications profile, use this one for routing when you want to force classification-based routing.
        -- this uses the default profile as a base.
        name = "classifications",
        function_name = "factor_and_speed_classifications",
        metric = "custom"
    },
    { -- this is the classifications profile, use this one for routing when you want to aggressively force classification-based routing.
        -- this uses the default profile as a base.
        name = "classifications_aggressive",
        function_name = "factor_and_speed_classifications_aggressive",
        metric = "custom"
    }
}

-- interprets access tags
function can_access (attributes, result)
    local last_access = {
        factor = nil,
        anyways = false 
    }
    
    -- first do access=x.
    local access = access_values[attributes.access]
    if access != nil then
        result.attributes_to_keep.access = true
        last_access.factor = access
    end
        
    -- then do motor_vehicle=x, etc.. based on the vehicle types above.    
    for i = 0, 10 do
        local access_key_key = vehicle_types[i]
        local access_key = attributes[access_key_key]
        if access_key then
            access = access_values[access_key]
            if access != nil then
                result.attributes_to_keep[access_key_key] = true
                last_access.factor = access
            end
        end
    end
    
    -- first do anyways:access=x.
    local access = access_values[attributes["anyways:access"]]
    if access != nil then
        result.attributes_to_keep["anyways:access"] = true
        last_access.factor = access
        last_access.anyways = true
    end

    -- then do the anyways overrides anyways:motor_vehicle=x, anyways:hgv=x etc.. based on the vehicle types above.    
    for i = 0, 10 do
        local access_key_key = vehicle_types[i]
        if access_key_key != nil then
            access_key_key = "anyways:" .. access_key_key
            local access_key = attributes[access_key_key]
            if access_key then
                access = access_values[access_key]
                if access != nil then
                    result.attributes_to_keep[access_key_key] = true
                    last_access.factor = access
                    last_access.anyways = true
                end
            end
        end
    end
    return last_access
end

-- turns a oneway tag value into a direction
function is_oneway (attributes, name)
    local oneway = attributes[name]
    if oneway != nil then
        if oneway == "yes" or
            oneway == "true" or
            oneway == "1" then
            return 1
        end
        if oneway == "-1" then
            return 2
        end
    end
    return nil
end

-- the main function turning attributes into a factor_and_speed and a tag whitelist
function factor_and_speed (attributes, result)

    local highway = attributes.highway

    result.speed = 0
    result.direction = 0
    result.canstop = true
    result.attributes_to_keep = {}

    -- set highway to ferry when ferry.
    local route = attributes.route;
    if route == "ferry" then
        highway = "ferry"
        result.attributes_to_keep.route = highway
    end

    -- get default speed profiles
    local highway_speed = speed_profile[highway]
    if highway_speed then
        result.speed = highway_speed
        result.direction = 0
        result.canstop = true
        result.attributes_to_keep.highway = highway
        if highway == "motorway" or
                highway == "motorway_link" then
            result.canstop = false
        end
    else
        return
    end

    -- get maxspeed if any and adjust speed accordingly.
    if attributes.maxspeed then
        local speed = itinero.parsespeed(attributes.maxspeed)
        if speed then
            result.speed = speed
            result.attributes_to_keep.maxspeed = true
        end
    end
    
    -- speed has been determined, now determine factor.
    -- a lower factor leads to lower weight for an edge.
    if result.speed == 0 then
        return
    end
    result.factor = 1.0 / (result.speed / 3.6)

    -- interpret access tags
    local access_factor = can_access(attributes, result)
    if access_factor.factor == 0 then
        -- only completely avoid when access factor is zero.
        result.speed = 0
        result.direction = 0
        result.canstop = true
        return
    end
    if access_factor.factor == nil then
        access_factor.factor = 1
    end
    if not access_factor.anyways then
        -- access was not determined by anyways access tags.
        
        -- remove access to construction roads
        if attributes["anyways:construction"] then
            result.speed = 0
            result.direction = 0
            result.canstop = false
            result.attributes_to_keep["anyways:construction"] = true
            return
        end
    end
    result.factor = result.factor / access_factor.factor

    -- get directional information
    local junction = attributes.junction
    if junction == "roundabout" then
        result.direction = 1
        result.attributes_to_keep.junction = true
    end
    local direction = is_oneway (attributes, "oneway")
    if direction != nil then
        result.direction = direction
        result.attributes_to_keep.oneway = true
    end
end

-- multiplication factors per classification
classifications_factors = {
    ["motorway"] = 10,
    ["motorway_link"] = 10,
    ["trunk"] = 9,
    ["trunk_link"] = 9,
    ["primary"] = 8,
    ["primary_link"] = 8,
    ["secondary"] = 7,
    ["secondary_link"] = 7,
    ["tertiary"] = 6,
    ["tertiary_link"] = 6,
    ["unclassified"] = 5,
    ["residential"] = 5
}

-- the classifications function for the classifications profile
function factor_and_speed_classifications (attributes, result)

    factor_and_speed(attributes, result)

    if result.speed == 0 then
        return
    end

    -- result.factor = 1.0 / (result.speed / 3.6)
    local classification_factor = classifications_factors[attributes.highway]
    if classification_factor != nil then
        result.factor = result.factor / classification_factor
    else
        result.factor = result.factor / 4
    end
end

-- multiplication factors per classification
classifications_factors_aggressive = {
    ["motorway"] = 18,
    ["motorway_link"] = 18,
    ["trunk"] = 12,
    ["trunk_link"] = 12,
    ["primary"] = 8,
    ["primary_link"] = 8,
    ["secondary"] = 5,
    ["secondary_link"] = 5,
    ["tertiary"] = 3,
    ["tertiary_link"] = 3,
    ["unclassified"] = 2,
    ["residential"] = 2
}

-- the classifications function for the classifications profile
function factor_and_speed_classifications_aggressive (attributes, result)

    factor_and_speed(attributes, result)

    if result.speed == 0 then
        return
    end

    -- result.factor = 1.0 / (result.speed / 3.6)
    local classification_factor = classifications_factors_aggressive[attributes.highway]
    if classification_factor != nil then
        result.factor = result.factor / classification_factor
    else
        result.factor = result.factor
    end
end

-- instruction generators
instruction_generators = {
    {
        applies_to = "", -- applies to all profiles when empty
        generators = {
            {
                name = "start",
                function_name = "get_start"
            },
            {
                name = "stop",
                function_name = "get_stop"
            },
            {
                name = "roundabout",
                function_name = "get_roundabout"
            },
            {
                name = "turn",
                function_name = "get_turn"
            }
        }
    }
}

-- gets the first instruction
function get_start (route_position, language_reference, instruction)
    if route_position.is_first() then
        local direction = route_position.direction()
        instruction.text = itinero.format(language_reference.get("Start {0}."), language_reference.get(direction));
        instruction.shape = route_position.shape
        return 1
    end
    return 0
end

-- gets the last instruction
function get_stop (route_position, language_reference, instruction)
    if route_position.is_last() then
        instruction.text = language_reference.get("Arrived at destination.");
        instruction.shape = route_position.shape
        return 1
    end
    return 0
end

function contains (attributes, key, value)
    if attributes then
        return localvalue == attributes[key];
    end
end

-- gets a roundabout instruction
function get_roundabout (route_position, language_reference, instruction)
    if route_position.attributes.junction == "roundabout" and
            (not route_position.is_last()) then
        local attributes = route_position.next().attributes
        if attributes.junction then
        else
            local exit = 1
            local count = 1
            local previous = route_position.previous()
            while previous and previous.attributes.junction == "roundabout" do
                local branches = previous.branches
                if branches then
                    branches = branches.get_traversable()
                    if branches.count > 0 then
                        exit = exit + 1
                    end
                end
                count = count + 1
                previous = previous.previous()
            end

            instruction.text = itinero.format(language_reference.get("Take the {0}th exit at the next roundabout."), "" .. exit)
            if exit == 1 then
                instruction.text = itinero.format(language_reference.get("Take the first exit at the next roundabout."))
            elseif exit == 2 then
                instruction.text = itinero.format(language_reference.get("Take the second exit at the next roundabout."))
            elseif exit == 3 then
                instruction.text = itinero.format(language_reference.get("Take the third exit at the next roundabout."))
            end
            instruction.type = "roundabout"
            instruction.shape = route_position.shape
            return count
        end
    end
    return 0
end

-- gets a turn
function get_turn (route_position, language_reference, instruction)
    local relative_direction = route_position.relative_direction().direction

    local turn_relevant = false
    local branches = route_position.branches
    if branches then
        branches = branches.get_traversable()
        if relative_direction == "straighton" and
                branches.count >= 2 then
            turn_relevant = true -- straight on at cross road
        end
        if relative_direction != "straighton" and
    branches.count > 0 then
    turn_relevant = true -- an actual normal turn
        end
    end

    if turn_relevant then
        local next = route_position.next()
        local name = nil
        if next then
            name = next.attributes.name
        end
        if name then
            instruction.text = itinero.format(language_reference.get("Go {0} on {1}."),
                    language_reference.get(relative_direction), name)
            instruction.shape = route_position.shape
        else
            instruction.text = itinero.format(language_reference.get("Go {0}."),
                    language_reference.get(relative_direction))
            instruction.shape = route_position.shape
        end

        return 1
    end
    return 0
end