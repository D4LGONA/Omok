#pragma once
#include "stdafx.h"
#include "Session.h"

void HandleQueuePacket(Session& player, const CS_QUEUE_PACKET& pkt);
void HandleMatchingResponse(Session& player, const CS_MATCHING_RESPONSE_PACKET& pkt);
void HandleDisconnectMatchQueue(Session& player);