#version 410 core

layout (location = 0) out vec2 ShadowMapOP;
layout (location = 1) out vec3 DistanceMapOP;
layout (location = 2) out vec4 NormalDepthOP;

in VS_OUT {
    vec3 FragPos;
    vec3 Normal;
    vec2 TexCoords;
    vec4 FragPosLightSpace;
} fs_in;

uniform sampler2D shadowMap;
uniform sampler2D dilatedShadowMap;

uniform vec3 lightPos;
uniform vec3 viewPos;

float bias;

float retDistance(vec3 a, vec3 b)
{
	return sqrt((b.x - a.x) * (b.x - a.x) + (b.y - a.y) * (b.y - a.y) + (b.z - a.z) * (b.z - a.z));
}

vec3 returnDistanceMap(vec4 fragposLightSpace)
{
	vec3 projCoords = fragposLightSpace.xyz / fragposLightSpace.w;
    // transform to [0,1] range
    projCoords = projCoords * 0.5 + 0.5;
    // get closest depth value from light's perspective (using [0,1] range fragPosLight as coords)
    float closestDepth = texture(dilatedShadowMap, projCoords.xy).r; 
    // get depth of current fragment from light's perspective
    float currentDepth = projCoords.z;

	vec3 ret;

	ret.x = currentDepth - closestDepth;
	if (projCoords.z > 1.0)
		ret.x = 0.0;

	ret.y = retDistance(viewPos, fs_in.FragPos);
	//ret.z = ((currentDepth - closestDepth) >= 0.0) ? 0.0 : 1.0;
	if(ret.x > 0.11)//Needs to be worked on!
		ret.z = 1.0;
	else
		ret.z = 0.0;
	return ret;
}

float ShadowCalculation(vec4 fragPosLightSpace, vec3 normal, vec3 lightDir)
{
    // perform perspective divide
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    // transform to [0,1] range
    projCoords = projCoords * 0.5 + 0.5;
    // get closest depth value from light's perspective (using [0,1] range fragPosLight as coords)
    float closestDepth = texture(shadowMap, projCoords.xy).r; 
    // get depth of current fragment from light's perspective
    float currentDepth = projCoords.z;
    // check whether current frag pos is in shadow
	
    float shadow = currentDepth - bias > closestDepth  ? 1.0 : 0.0;  

	if(projCoords.z > 1.0)
    shadow = 0.0;

    return shadow;
}

void main()
{           
    vec3 normal = normalize(fs_in.Normal);
    vec3 lightDir = normalize(lightPos - fs_in.FragPos);
	bias = max(0.05 * (1.0 - dot(normal, lightDir)), 0.005);  
	vec3 distanceMap = returnDistanceMap(fs_in.FragPosLightSpace);
    float shadow = ShadowCalculation(fs_in.FragPosLightSpace, normal, lightDir);                        
	ShadowMapOP = vec2(shadow, 0.0);
	DistanceMapOP = distanceMap;
	if(gl_FragCoord.z > 1.0)
		NormalDepthOP = vec4 (normal, 0.0);
	else
		NormalDepthOP = vec4 (normal, gl_FragCoord.z);
}