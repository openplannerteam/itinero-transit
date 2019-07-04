
name = "pedestrian"
vehicle_types = { "foot" }

minspeed = 4
maxspeed = 5

speed_profile = {
	["primary"] = 4,
	["primary_link"] = 4,
	["secondary"] = 4,
	["secondary_link"] = 4,
	["tertiary"] = 4,
	["tertiary_link"] = 4,
	["unclassified"] = 4,
	["residential"] = 4,
	["service"] = 4,
	["services"] = 4,
	["road"] = 4,
	["track"] = 4,
	["cycleway"] = 4,
	["path"] = 4,
	["footway"] = 4,
	["pedestrian"] = 4,
	["living_street"] = 4,
	["ferry"] = 4,
	["movable"] = 4,
	["shuttle_train"] = 4,
  	["default"] = 4
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
    ["destination"] = access_factor_yes,
    ["delivery"] = access_factor_avoid,
    ["service"] = access_factor_avoid,
    ["customers"] = access_factor_yes,
    ["private"] = access_factor_local,
    ["no"] = access_factor_no,
    ["use_sidepath"] = access_factor_no,
    ["gate"] = access_factor_no,
    ["bollard"] = access_factor_no
}

profile_whitelist = {
	"highway",
	"foot",
	"access",
	"anyways:construction",
	"anyways:access",
	"anyways:foot"
}

meta_whitelist = {
	"name"
}

profiles = {
	{
		name = "",
		function_name = "factor_and_speed",
		metric = "time"
	},
	{ 
		name = "shortest",
		function_name = "factor_and_speed",
		metric = "distance",
	},
	{
		name = "default",
		function_name = "factor_and_speed",
		metric = "custom"
	},
    { -- this is the OPA profile, use this one for OPA routing.
        name = "opa",
        function_name = "factor_and_speed_opa",
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

	 local highway_speed = speed_profile[highway]
	 if highway_speed then
        result.speed = highway_speed
        result.direction = 0
		result.canstop = true
		result.attributes_to_keep.highway = highway
	 else
	    return
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
end


highest_avoid_factor = 0.5
avoid_factor = 0.7
prefer_factor = 2
highest_prefer_factor = 3

-- multiplication factors per classification (balanced)
balanced_factors = {
	["primary"] = highest_avoid_factor,
	["primary_link"] = highest_avoid_factor,
	["secondary"] = avoid_factor,
	["secondary_link"] = avoid_factor,
	["tertiary"] = avoid_factor,
	["tertiary_link"] = avoid_factor,
	["residential"] = 1,
	["path"] = prefer_factor,
	["cycleway"] = prefer_factor,
	["footway"] = highest_prefer_factor,
	["pedestrian"] = highest_prefer_factor,
	["steps"] = prefer_factor
}

-- the factor function for the factor profile
function factor_and_speed_opa (attributes, result)

	factor_and_speed (attributes, result)

	if result.speed == 0 then
		return
	end

	-- result.factor = 1.0 / (result.speed / 3.6)
	local balanced_factor = balanced_factors[attributes.highway]
	if balanced_factor ~= nil then
		result.factor = result.factor / balanced_factor
	else
		balanced_factor = 1
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
		if  relative_direction != "straighton" and 
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