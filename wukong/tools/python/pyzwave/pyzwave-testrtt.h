#ifndef TESTRTT_H
#define TESTRTT_H

extern char PyZwave_messagebuffer[1024];
extern int PyZwave_src;
extern int PyZwave_print_debug_info;
extern int verbose;
extern int pyzwave_basic;
extern int pyzwave_generic;
extern int pyzwave_specific;
int PyZWave_send(unsigned id,unsigned char *in,int len);
int PyZwave_init_usb(char *dev_name);
int PyZwave_init(char *host);
int PyZwave_receive(int wait_msec);
int PyZwave_send(unsigned id,unsigned char *in,int len);
void zwave_check_state(unsigned char c);
int ZW_AddNodeToNetwork(int mode);
int ZW_RemoveNodeFromNetwork(int mode);
int PyZwave_discover(void);
int ZW_SetDefault(void);
unsigned long PyZwave_get_addr(void);
int PyZwave_is_node_fail(int node_id);
int PyZwave_getDeviceType(unsigned node_id);
char *PyZwave_status(void);
void PyZwave_clearstatus(void);
int PyZwave_zwavefd(void);
void PyZwave_routing(unsigned node_id);
int PyZwave_hard_reset(void);
#endif
